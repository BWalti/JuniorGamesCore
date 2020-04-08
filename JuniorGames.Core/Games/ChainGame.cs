namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Serilog;
    using Stateless;

    /// <summary>
    ///     This game starts with displaying a "chain" (starting with one element) which you have to type in afterwards.
    ///     If you re-type it correctly, then all LEDs will blink once, the chain will be extended by one more LED and
    ///     displayed again.
    ///     If you did not re-type it correctly and you still have a re-try left, the chain gets displayed again and you may
    ///     try again.
    ///     If you did not re-type it correctly and you have no re-tries left, a new game will be started (if the maximum
    ///     number of games has not been reached).
    /// </summary>
    public class ChainGame : GameBase
    {
        private readonly List<ILightableButton> chain;
        private readonly ChainGameOptions options;
        private readonly Random random;
        private int games;
        private int index;
        private int retries;
        private StateMachine<ChainGameState, ChainGameEvent> stateMachine;

        public ChainGame(IGameBox box, ChainGameOptions options) : base(box)
        {
            this.random = new Random();
            this.chain = new List<ILightableButton>();

            this.options = options;
        }

        protected override async Task OnStart()
        {
            Log.Information("Starting ChainGame...");

            this.stateMachine = new StateMachine<ChainGameState, ChainGameEvent>(ChainGameState.Initialized);
            this.stateMachine.Configure(ChainGameState.Initialized)
                .Permit(ChainGameEvent.Yes, ChainGameState.StepAdded)
                .OnEntryAsync(this.Initialize);

            this.stateMachine.Configure(ChainGameState.StepAdded)
                .Permit(ChainGameEvent.Yes, ChainGameState.ChainDisplayed)
                .OnEntryAsync(this.DisplayChain);

            this.stateMachine.Configure(ChainGameState.ChainDisplayed)
                .Permit(ChainGameEvent.Yes, ChainGameState.InputGood)
                .OnEntryAsync(this.CheckOneInput);

            this.stateMachine.Configure(ChainGameState.InputGood)
                .Permit(ChainGameEvent.Yes,ChainGameState.IsEndOfChain)
                .OnEntryAsync(this.CheckEndOfChain)
                .Permit(ChainGameEvent.No,ChainGameState.HasRetryLeft)
                .OnEntryAsync(this.HasRetryLeft);

            this.stateMachine.Configure(ChainGameState.IsEndOfChain)
                .Permit(ChainGameEvent.Yes, ChainGameState.StepAdded)
                .OnEntryAsync(this.AddStep)
                .Permit(ChainGameEvent.No, ChainGameState.InputGood)
                .OnEntryAsync(this.CheckOneInput);

            this.stateMachine.Configure(ChainGameState.HasRetryLeft)
                .Permit(ChainGameEvent.Yes, ChainGameState.ChainDisplayed)
                .OnEntryAsync(this.DisplayChain)
                .Permit(ChainGameEvent.No, ChainGameState.HasGameLeft)
                .OnEntryAsync(this.HasGameLeft);

            this.stateMachine.Configure(ChainGameState.HasGameLeft)
                .Permit(ChainGameEvent.Yes, ChainGameState.ChainCleared)
                .OnEntryAsync(this.Initialize)
                .Permit(ChainGameEvent.No, ChainGameState.Finished)
                .OnEntryAsync(this.Finished);

            this.stateMachine.Configure(ChainGameState.ChainCleared)
                .Permit(ChainGameEvent.Yes, ChainGameState.StepAdded)
                .OnEntryAsync(this.AddStep);

            await this.stateMachine.FireAsync(ChainGameEvent.Yes);
        }

        private async Task Finished()
        {
            await this.stateMachine.DeactivateAsync();
        }

        private async Task Initialize()
        {
            this.retries = 0;
            this.games++;
            this.chain.Clear();

            await this.AddStep();
        }

        private async Task HasGameLeft()
        {
            this.CancellationToken.ThrowIfCancellationRequested();
            if (this.games < this.options.Games)
            {
                await this.stateMachine.FireAsync(ChainGameEvent.Yes);
            }
            else
            {
                await this.stateMachine.FireAsync(ChainGameEvent.No);
            }
        }

        private async Task HasRetryLeft()
        {
            this.CancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(500);
            await this.Bad();

            if (this.retries++ < this.options.Retries)
            {
                await this.stateMachine.FireAsync(ChainGameEvent.Yes);
            }
            else
            {
                await this.stateMachine.FireAsync(ChainGameEvent.No);
            }
        }

        private async Task CheckEndOfChain()
        {
            this.CancellationToken.ThrowIfCancellationRequested();
            if (this.index == this.chain.Count)
            {
                await Task.Delay(500);
                await this.Good();
                await this.stateMachine.FireAsync(ChainGameEvent.Yes);
            }
            else
            {
                await this.stateMachine.FireAsync(ChainGameEvent.No);
            }
        }

        private async Task CheckOneInput()
        {
            this.CancellationToken.ThrowIfCancellationRequested();
            var chainId = this.chain[this.index++].ButtonIdentifier;

            try
            {
                using (this.GameBox.LightButtonOnPress())
                {
                    var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var result = await this.GameBox
                        .OnButtonDown
                        .FirstAsync().RunAsync(tokenSource.Token);

                    this.CancellationToken.ThrowIfCancellationRequested();

                    // now await until it's not pressed anymore:
                    await this.GameBox.OnButtonUp.FirstAsync(lbpp => lbpp.Equals(result)).RunAsync(tokenSource.Token);

                    if (chainId.Equals(result))
                    {
                        await this.stateMachine.FireAsync(ChainGameEvent.Yes);
                        return;
                    }

                    await this.stateMachine.FireAsync(ChainGameEvent.No);
                }
            }
            catch (Exception)
            {
                await this.stateMachine.FireAsync(ChainGameEvent.No);
            }
        }

        private async Task DisplayChain()
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            // reset index, as after everytime the chain is displayed, the user needs to re-enter the code...
            this.index = 0;

            await Task.Delay(500);

            foreach (var led in this.chain)
            {
                await led.SetLight(true, 400);
                await Task.Delay(400);
            }

            await this.stateMachine.FireAsync(ChainGameEvent.Yes);
        }

        private async Task AddStep()
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            var randomButton = this.RandomButton();
            Log.Information("Adding '{@Identifier}' to the chain", randomButton.ButtonIdentifier);

            this.chain.Add(randomButton);

            await this.stateMachine.FireAsync(ChainGameEvent.Yes);
        }

        private ILightableButton RandomButton()
        {
            var all = this.GameBox.LedButtonPinPins.ToList();
            var randomIndex = this.random.Next(all.Count);

            return all[randomIndex];
        }

        private async Task Good()
        {
            await this.GameBox.BlinkAll(1, 500);
        }

        private async Task Bad()
        {
            await this.GameBox.BlinkAll(2);
        }
    }
}