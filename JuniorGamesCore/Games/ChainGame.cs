namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Appccelerate.StateMachine;
    using JuniorGames.Core.Framework;
    using Serilog;

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
        private AsyncPassiveStateMachine<ChainGameState, ChainGameEvent> stateMachine;

        public ChainGame(IGameBox box, ChainGameOptions options) : base(box)
        {
            this.random = new Random();
            this.chain = new List<ILightableButton>();

            this.options = options;
        }

        protected override async Task Start()
        {
            Log.Information("Starting ChainGame...");

            this.stateMachine = new AsyncPassiveStateMachine<ChainGameState, ChainGameEvent>();
            this.stateMachine.In(ChainGameState.Initialized)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.StepAdded)
                .Execute(this.Initialize);

            this.stateMachine.In(ChainGameState.StepAdded)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.ChainDisplayed)
                .Execute(this.DisplayChain);

            this.stateMachine.In(ChainGameState.ChainDisplayed)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.InputGood)
                .Execute(this.CheckOneInput);

            this.stateMachine.In(ChainGameState.InputGood)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.IsEndOfChain)
                .Execute(this.CheckEndOfChain)
                .On(ChainGameEvent.No)
                .Goto(ChainGameState.HasRetryLeft)
                .Execute(this.HasRetryLeft);

            this.stateMachine.In(ChainGameState.IsEndOfChain)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.StepAdded)
                .Execute(this.AddStep)
                .On(ChainGameEvent.No)
                .Goto(ChainGameState.InputGood)
                .Execute(this.CheckOneInput);

            this.stateMachine.In(ChainGameState.HasRetryLeft)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.ChainDisplayed)
                .Execute(this.DisplayChain)
                .On(ChainGameEvent.No)
                .Goto(ChainGameState.HasGameLeft)
                .Execute(this.HasGameLeft);

            this.stateMachine.In(ChainGameState.HasGameLeft)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.ChainCleared)
                .Execute(this.Initialize)
                .On(ChainGameEvent.No)
                .Goto(ChainGameState.Finished)
                .Execute(this.Finished);

            this.stateMachine.In(ChainGameState.ChainCleared)
                .On(ChainGameEvent.Yes)
                .Goto(ChainGameState.StepAdded)
                .Execute(this.AddStep);

            this.stateMachine.Initialize(ChainGameState.Initialized);
            await this.stateMachine.Start();
            await this.stateMachine.Fire(ChainGameEvent.Yes);
        }

        private Task Finished()
        {
            this.stateMachine.Stop();
            return Task.CompletedTask;
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
                await this.stateMachine.Fire(ChainGameEvent.Yes);
            }
            else
            {
                await this.stateMachine.Fire(ChainGameEvent.No);
            }
        }

        private async Task HasRetryLeft()
        {
            this.CancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(500);
            await this.Bad();

            if (this.retries++ < this.options.Retries)
            {
                await this.stateMachine.Fire(ChainGameEvent.Yes);
            }
            else
            {
                await this.stateMachine.Fire(ChainGameEvent.No);
            }
        }

        private async Task CheckEndOfChain()
        {
            this.CancellationToken.ThrowIfCancellationRequested();
            if (this.index == this.chain.Count)
            {
                await Task.Delay(500);
                await this.Good();
                await this.stateMachine.Fire(ChainGameEvent.Yes);
            }
            else
            {
                await this.stateMachine.Fire(ChainGameEvent.No);
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

                    // now await, that it's not pressed anymore:
                    await this.GameBox.OnButtonUp.FirstAsync(lbpp => lbpp.Equals(result)).RunAsync(tokenSource.Token);

                    if (chainId.Equals(result))
                    {
                        await this.stateMachine.Fire(ChainGameEvent.Yes);
                        return;
                    }

                    await this.stateMachine.Fire(ChainGameEvent.No);
                }
            }
            catch (Exception)
            {
                await this.stateMachine.Fire(ChainGameEvent.No);
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

            await this.stateMachine.Fire(ChainGameEvent.Yes);
        }

        private async Task AddStep()
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            var randomButton = this.RandomButton();
            Log.Information("Adding '{@Identifier}' to the chain", randomButton.ButtonIdentifier);

            this.chain.Add(randomButton);

            await this.stateMachine.Fire(ChainGameEvent.Yes);
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