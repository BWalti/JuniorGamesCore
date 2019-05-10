namespace SimulatorBox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;

    public sealed class GameBoxSimulator : GameBoxBase
    {
        private readonly Dictionary<ButtonIdentifier, ILightableButton> lookup;

        public GameBoxSimulator(GameBoxOptions options)
        : base(options)
        {
            var models = new[]
            {
                new LightyButtonModel(GreenOneButtonIdentifier), new LightyButtonModel(YellowOneButtonIdentifier),
                new LightyButtonModel(RedOneButtonIdentifier), new LightyButtonModel(BlueOneButtonIdentifier),
                new LightyButtonModel(WhiteOneButtonIdentifier), new LightyButtonModel(WhiteTwoButtonIdentifier),
                new LightyButtonModel(BlueTwoButtonIdentifier), new LightyButtonModel(RedTwoButtonIdentifier),
                new LightyButtonModel(YellowTwoButtonIdentifier), new LightyButtonModel(GreenTwoButtonIdentifier),
            };

            this.LedButtonPinPins = models.ToList();

            this.lookup = this.LedButtonPinPins.ToDictionary(lbpp => lbpp.ButtonIdentifier, lbpp => lbpp);

            this.IdleTimer = this.LedButtonPinPins.Select(lbpp => lbpp.Button).Merge().Select(s => false)
            .Throttle(TimeSpan.FromSeconds(this.Options.IdleTimeout));

            this.OnButtonDown = this.LedButtonPinPins.Select(lbpp => lbpp.ButtonDown).Merge();

            this.OnButtonUp = this.LedButtonPinPins.Select(lbpp => lbpp.ButtonUp).Merge();

            this.OnButton = this.LedButtonPinPins.Select(lbpp => lbpp.Button).Merge();
        }

        public override IObservable<ButtonPressedEventArgs> OnButton { get; }
        
        public override IObservable<IEnumerable<ButtonPressedEventArgs>> Reset { get; }

        public override IObservable<ButtonIdentifier> OnButtonUp { get; }

        public override IObservable<ButtonIdentifier> OnButtonDown { get; }

        public override IObservable<bool> IdleTimer { get; set; }

        public override IEnumerable<ILightableButton> LedButtonPinPins { get; }

        public override ILightableButton this[ButtonIdentifier buttonIdentifier] => this.lookup[buttonIdentifier];

        public override IObservable<ButtonIdentifier> WaitForNextButton(TimeSpan timeout)
        {
            var observable = this.LedButtonPinPins.Select(lbpp => lbpp.ButtonDown).Merge().Timeout(timeout);

            return observable;
        }

        public override async Task Blink(IEnumerable<ButtonIdentifier> buttons, int times = 1, int duration = 200)
        {
            if (times < 1) return;

            var lightableButtons = this.GetLightableButtonsForIdentifiers(buttons);

            for (var i = 0; i < times; i++)
            {
                await Task.WhenAll(lightableButtons.Select(b => b.SetLight(true, duration)));

                if (i != times - 1) await Task.Delay(duration);
            }
        }

        public override async Task BlinkAll(int times = 1, int duration = 200)
        {
            if (times < 1) return;

            for (var i = 0; i < times; i++)
            {
                await this.SetAll(true, duration);

                if (i != times - 1) await Task.Delay(duration);
            }
        }

        public override IDisposable LightButtonOnPress()
        {
            var subscriptions = this.LedButtonPinPins
            .Select(lbpp => lbpp.Button.Subscribe(args => this.LightifyOnPress(lbpp, args))).ToArray();

            return new CollectionDisposableWithFinalAction(async () => await this.SetAll(false), subscriptions);
        }

        private async void LightifyOnPress(ILightableButton lbpp, ButtonPressedEventArgs args)
        {
            await lbpp.SetLight(args.IsPressed);
        }

        public override async Task SetAll(bool enabled, int? duration = null)
        {
            await Task.WhenAll(this.LedButtonPinPins.Select(lbpp => lbpp.SetLight(enabled, duration)));
        }

        public override async Task Set(ButtonIdentifier button, bool enabled, int? milliseconds = null)
        {
            var lightableButton = this.lookup[button];
            await lightableButton.SetLight(enabled, milliseconds);
        }

        public override async Task Set(IEnumerable<ButtonIdentifier> buttons, bool enabled, int? milliseconds = null)
        {
            var lightableButtons = this.GetLightableButtonsForIdentifiers(buttons);
            await Task.WhenAll(lightableButtons.Select(lb => lb.SetLight(enabled, milliseconds)));
        }

        private List<ILightableButton> GetLightableButtonsForIdentifiers(IEnumerable<ButtonIdentifier> buttons)
        {
            return buttons.Select(b => this.lookup[b]).ToList();
        }
    }
}