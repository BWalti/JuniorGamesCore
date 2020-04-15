﻿namespace JuniorGames.GamesClean
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using GameBox.Framework;
    using Serilog;
    using Stateless;

    public class SimpleGameOptions
    {
        public TimeSpan Pause { get; set; }
        public TimeSpan LightUp { get; set; }
        public int Retries { get; set; }
    }

    public class SimpleGame
    {

        public class SimpleGameStatus
        {
            public SimpleGameStatus(List<ILightableButton> chain)
            {
                this.Chain = chain;
                this.FaultCounter = 0;
                this.InputIndex = 0;
            }

            public int InputIndex { get; private set; }

            public List<ILightableButton> Chain { get; }

            public int FaultCounter { get; private set; }

            public ButtonIdentifier ExpectedButton => this.Chain[this.InputIndex].ButtonIdentifier;

            internal void IncreaseFaultCounter()
            {
                this.FaultCounter++;
            }

            internal void IncreaseInputIndex()
            {
                this.InputIndex++;
            }

            internal void ResetInputIndex()
            {
                this.InputIndex = 0;
            }

            public void ResetFaultCounter()
            {
                this.FaultCounter = 0;
            }
        }

        private SimpleGameOptions Options { get; }
        
        private readonly IBox box;

        private readonly StateMachine<SimpleGameState, SimpleGameEvent>.TriggerWithParameters<ButtonIdentifier>
            buttonPressedTrigger;

        private readonly ReadOnlyCollection<ILightableButton> ledButtons;
        private readonly Random random = new Random();
        private readonly StateMachine<SimpleGameState, SimpleGameEvent> stateMachine;
        private readonly TaskCompletionSource<object> taskCompletionSource;
        
        private IDisposable buttonPressedSubscription;
        private IDisposable lightButtonOnPressSubscription;

        public SimpleGameStatus Status { get; private set; }

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
                .Permit(SimpleGameEvent.Finish, SimpleGameState.Finished);

            this.stateMachine.Configure(SimpleGameState.Fault)
                .OnEntryAsync(this.OnFault)
                .Permit(SimpleGameEvent.DisplayChain, SimpleGameState.DisplayChain)
                .Permit(SimpleGameEvent.Finish, SimpleGameState.Finished);

            this.stateMachine.Configure(SimpleGameState.Finished)
                .OnEntryAsync(this.OnFinished)
                .Permit(SimpleGameEvent.DisplayChain, SimpleGameState.DisplayChain);
        }

        public Task Result => this.taskCompletionSource.Task;

        private async Task OnFinished()
        {
            //this.taskCompletionSource.SetResult(1);
            await this.InitializeChain();
        }

        private async Task OnFault()
        {
            Log.Information("OnFault");
            this.DisposeButtonSubscriptions();

            await Task.Delay(this.Options.Pause);
            await this.box.BlinkAll(2);

            this.Status.IncreaseFaultCounter();
            if (this.Status.FaultCounter < this.Options.Retries)
            {
                await this.DoublePause();
                await this.stateMachine.FireAsync(SimpleGameEvent.DisplayChain);
            }
            else
            {
                await this.stateMachine.FireAsync(SimpleGameEvent.Finish);
            }
        }

        private void DisposeButtonSubscriptions()
        {
            this.lightButtonOnPressSubscription.Dispose();
            this.buttonPressedSubscription.Dispose();
        }

        private async Task DoublePause()
        {
            Log.Information("DoublePause");
            await Task.Delay(this.Options.Pause);
            await Task.Delay(this.Options.Pause);
        }

        private async Task OnSuccess()
        {
            Log.Information("OnSuccess");
            this.DisposeButtonSubscriptions();
            this.Status.ResetFaultCounter();

            var newButton = this.PickRandomButton();
            this.Status.Chain.Add(newButton);

            await Task.Delay(this.Options.Pause);
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

        private async Task ButtonPressed(ButtonIdentifier arg)
        {
            Log.Information($"ButtonPressed: {arg}");

            if (this.Status.ExpectedButton.Equals(arg))
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
            Log.Information("DoDisplayChain");

            foreach (var led in this.Status.Chain)
            {
                await led.SetLight(true, this.Options.LightUp);
                await Task.Delay(this.Options.Pause);
            }

            await this.stateMachine.FireAsync(SimpleGameEvent.ChainDisplayed);
        }

        private async Task InitializeChain()
        {
            Log.Information("Initialize Chain");

            var chain = Enumerable.Range(0, 3)
                .Select(_ => this.PickRandomButton())
                .ToList();

            this.Status = new SimpleGameStatus(chain);
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