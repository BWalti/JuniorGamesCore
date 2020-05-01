namespace JuniorGames.GamesClean
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using GameBox.Framework;
    using Serilog;
    using Stateless;

    public class SimpleGame
    {
        private readonly IBox box;

        private readonly StateMachine<SimpleGameState, SimpleGameEvent>.TriggerWithParameters<ButtonIdentifier>
            buttonPressedTrigger;

        private readonly ReadOnlyCollection<ILightableButton> ledButtons;
        private readonly Random random = new Random();
        private readonly StateMachine<SimpleGameState, SimpleGameEvent> stateMachine;
        private readonly TaskCompletionSource<object> taskCompletionSource;

        private IDisposable buttonPressedSubscription;
        private IDisposable lightButtonOnPressSubscription;
        private SimpleGameTimeSpanCalculator timeSpanCalculator;

        public SimpleGame(IBox box, SimpleGameOptions options)
        {
            this.Options = options;
            this.taskCompletionSource = new TaskCompletionSource<object>();

            this.box = box;
            this.ledButtons = this.box.LedButtonPinPins.ToList().AsReadOnly();

            this.stateMachine = new StateMachine<SimpleGameState, SimpleGameEvent>(SimpleGameState.Init);
            this.stateMachine.Configure(SimpleGameState.Init)
                .Permit(SimpleGameEvent.Start, SimpleGameState.Start);

            this.stateMachine.Configure(SimpleGameState.Start)
                .OnEntryAsync(this.InitializeChain)
                .Permit(SimpleGameEvent.DisplayChain, SimpleGameState.DisplayChain);

            this.stateMachine.Configure(SimpleGameState.DisplayChain)
                .OnEntryAsync(this.DoDisplayChain)
                .Permit(SimpleGameEvent.ChainDisplayed, SimpleGameState.AwaitInput);

            this.buttonPressedTrigger =
                this.stateMachine.SetTriggerParameters<ButtonIdentifier>(SimpleGameEvent.ButtonPressed);

            this.stateMachine.Configure(SimpleGameState.AwaitInput)
                .OnEntryFromAsync(SimpleGameEvent.ChainDisplayed, this.DoAwaitInput)
                .OnEntryFromAsync(this.buttonPressedTrigger, this.ButtonPressed)
                .PermitReentry(SimpleGameEvent.ButtonPressed)
                .Permit(SimpleGameEvent.ChainCorrectlyRepeated, SimpleGameState.Success)
                .Permit(SimpleGameEvent.ChainWronglyRepeated, SimpleGameState.Fault);

            this.stateMachine.Configure(SimpleGameState.Success)
                .OnEntryAsync(this.OnSuccess)
                .Permit(SimpleGameEvent.DisplayChain, SimpleGameState.DisplayChain)
                .Permit(SimpleGameEvent.Won, SimpleGameState.Won);

            this.stateMachine.Configure(SimpleGameState.Won)
                .OnEntryAsync(this.Won)
                .Permit(SimpleGameEvent.FinishChain, SimpleGameState.ChainFinished);

            this.stateMachine.Configure(SimpleGameState.Fault)
                .OnEntryAsync(this.OnFault)
                .Permit(SimpleGameEvent.DisplayChain, SimpleGameState.DisplayChain)
                .Permit(SimpleGameEvent.FinishChain, SimpleGameState.ChainFinished);

            this.stateMachine.Configure(SimpleGameState.ChainFinished)
                .OnEntryAsync(this.OnChainFinished)
                .Permit(SimpleGameEvent.DisplayChain, SimpleGameState.DisplayChain);
        }

        private SimpleGameOptions Options { get; }

        public SimpleGameStatus Status { get; private set; }

        public Task Result => this.taskCompletionSource.Task;

        private async Task Won()
        {
            await this.box.BlinkAll(5);
            var groups = this.ledButtons.GroupBy(lb => lb.Color);

            foreach (var g in groups)
            {
                await this.box.Set(g.Select(_ => _.ButtonIdentifier), true, this.Options.LightUp);
            }

            await this.box.SetAll(true, TimeSpan.FromSeconds(2));
            await this.DoublePause();
            await this.stateMachine.FireAsync(SimpleGameEvent.FinishChain);
        }

        private async Task OnChainFinished()
        {
            //this.taskCompletionSource.SetResult(1);
            await this.InitializeChain();
        }

        private async Task OnFault()
        {
            Log.Information("OnFault");
            this.DisposeButtonSubscriptions();

            await this.Pause();
            await this.box.BlinkAll(2);

            this.Status.IncreaseFaultCounter();
            if (this.Status.FaultCounter < this.Options.Retries)
            {
                await this.DoublePause();
                await this.stateMachine.FireAsync(SimpleGameEvent.DisplayChain);
            }
            else
            {
                await this.stateMachine.FireAsync(SimpleGameEvent.FinishChain);
            }
        }

        private async Task Pause()
        {
            var calculated = this.CalculatePauseDuration();

            await Task.Delay(calculated);
        }

        private TimeSpan CalculatePauseDuration()
        {
            return this.timeSpanCalculator.GetCurrentPauseDuration();
        }

        private void DisposeButtonSubscriptions()
        {
            this.lightButtonOnPressSubscription.Dispose();
            this.buttonPressedSubscription.Dispose();
        }

        private async Task DoublePause()
        {
            Log.Information("DoublePause");

            var calculated = this.CalculatePauseDuration();

            await Task.Delay(calculated);
            await Task.Delay(calculated);
        }

        private async Task OnSuccess()
        {
            Log.Information("OnSuccess");
            this.DisposeButtonSubscriptions();
            this.Status.ResetFaultCounter();

            if (this.Status.Chain.Count >= this.Options.MaxChainLength)
            {
                await this.stateMachine.FireAsync(SimpleGameEvent.Won);
                return;
            }

            var newButton = this.PickRandomButton();
            this.Status.Chain.Add(newButton);

            await this.Pause();
            await this.box.BlinkAll(1, this.Options.LightUp);
            await this.DoublePause();
            await this.stateMachine.FireAsync(SimpleGameEvent.DisplayChain);
        }

        private Task DoAwaitInput()
        {
            Log.Information("DoAwaitInput");
            this.Status.ResetInputIndex();
            this.lightButtonOnPressSubscription = this.box.LightButtonOnPress();
            this.buttonPressedSubscription = this.box.OnButtonUp.Subscribe(async identifier =>
            {
                await this.stateMachine.FireAsync(this.buttonPressedTrigger, identifier);
            });

            return Task.CompletedTask;
        }

        private async Task ButtonPressed(ButtonIdentifier identifier)
        {
            Log.Information("ButtonPressed: {@Identifier}", identifier);

            if (this.Status.ExpectedButton.Equals(identifier))
            {
                // correct! :)
                this.Status.IncreaseInputIndex();
                if (this.Status.InputIndex >= this.Status.Chain.Count)
                {
                    // finished!
                    await this.stateMachine.FireAsync(SimpleGameEvent.ChainCorrectlyRepeated);
                }
            }
            else
            {
                // wrong! :(
                await this.stateMachine.FireAsync(SimpleGameEvent.ChainWronglyRepeated);
            }
        }

        private async Task DoDisplayChain()
        {
            var chainButtons = this.Status.Chain.Select(b => b.ButtonIdentifier);
            Log.Information("DoDisplayChain: {@Chain}", chainButtons);

            var lightUp = this.timeSpanCalculator.GetCurrentLightUpDuration();

            foreach (var led in this.Status.Chain)
            {
                await led.SetLight(true, lightUp);
                await this.Pause();
            }

            await this.stateMachine.FireAsync(SimpleGameEvent.ChainDisplayed);
        }

        private async Task InitializeChain()
        {
            Log.Information("Initialize Chain");

            var chain = Enumerable.Range(0, this.Options.StartLength)
                .Select(_ => this.PickRandomButton())
                .ToList();

            this.Status = new SimpleGameStatus(chain);
            this.timeSpanCalculator = new SimpleGameTimeSpanCalculator(this.Options, this.Status);

            await this.stateMachine.FireAsync(SimpleGameEvent.DisplayChain);
        }

        public async Task Start()
        {
            Log.Information("Start");

            await this.stateMachine.FireAsync(SimpleGameEvent.Start);
        }

        private ILightableButton PickRandomButton()
        {
            Log.Information("PickRandomButton");

            var index = this.random.Next(this.ledButtons.Count);
            return this.ledButtons[index];
        }
    }
}