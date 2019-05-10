namespace JuniorGames
{
    using System;
    using System.Collections.Generic;
    using System.Device.Gpio;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Serilog;

    public sealed class GameBox : GameBoxBase
    {
        private readonly IDictionary<ButtonIdentifier, ILightableButton> lookup;

        public GameBox(GameBoxOptions options, GpioController gpioController) : base(options)
        {
            Log.Verbose("Constructing LED Button configs...");
            var lightableButtons = new[]
            {
                new LightableButtonConfig(18 /* 12 */, 27 /* 13 */, GreenOneButtonIdentifier),
                new LightableButtonConfig(23 /* 16 */, 22 /* 15 */, YellowOneButtonIdentifier),
                new LightableButtonConfig(24 /* 18 */, 10 /* 19 */, RedOneButtonIdentifier),
                new LightableButtonConfig(25 /* 22 */, 9 /* 21 */, BlueOneButtonIdentifier),
                new LightableButtonConfig(8 /* 24 */, 11 /* 23 */, WhiteOneButtonIdentifier),

                new LightableButtonConfig(7 /* 26 */, 5 /* 29 */, WhiteTwoButtonIdentifier),
                new LightableButtonConfig(12 /* 32 */, 6 /* 31 */, BlueTwoButtonIdentifier),
                new LightableButtonConfig(16 /* 36 */, 13 /* 33 */, RedTwoButtonIdentifier),
                new LightableButtonConfig(20 /* 38 */, 19 /* 35 */, YellowTwoButtonIdentifier),
                new LightableButtonConfig(21 /* 40 */, 26 /* 37 */, GreenTwoButtonIdentifier)
            };

            Log.Verbose("Generating LedButtonPinPins...");
            this.LedButtonPinPins = lightableButtons.Select(lb =>
            {
                Log.Verbose($"Opening LED Pin {lb.LedPin}, setting output...");
                gpioController.OpenPin(lb.LedPin, PinMode.Output);
                Log.Verbose($"Setting LED {lb.LedPin} to Low...");
                gpioController.Write(lb.LedPin, PinValue.Low);

                Log.Verbose($"Opening Button Pin {lb.ButtonPin} as Input...");
                gpioController.OpenPin(lb.ButtonPin, PinMode.InputPullDown);
                
                Log.Verbose("Wrap LightableButton");
                return new LightableButton(gpioController, lb);
            }).ToList();

            Log.Verbose("Creating lookup...");
            this.lookup = this.LedButtonPinPins.ToDictionary(
                lbpp => new ButtonIdentifier(lbpp.Player, lbpp.Color),
                lbpp => lbpp);

            Log.Verbose("Creating IdleTimer...");
            this.IdleTimer = this.LedButtonPinPins
                .Select(lbpp => lbpp.Button)
                .Merge()
                .Select(s => false)
                .Throttle(TimeSpan.FromSeconds(this.Options.IdleTimeout));

            this.OnButtonDown = this.LedButtonPinPins
                .Select(lbpp => lbpp.ButtonDown)
                .Merge();

            this.OnButtonUp = this.LedButtonPinPins
                .Select(lbpp => lbpp.ButtonUp)
                .Merge();

            this.OnButton = this.LedButtonPinPins
                .Select(lbpp => lbpp.Button)
                .Merge();

            var ctrlAltDeleteButtons = new[] {GreenOneButtonIdentifier, GreenTwoButtonIdentifier};

            var pressedCtrlAltDeleteButtons = ctrlAltDeleteButtons
                .Select(id => this.lookup[id].Button)
                .CombineLatest()
                .Throttle(TimeSpan.FromSeconds(1))
                .Where(latest => latest.All(button => button.IsPressed));

            pressedCtrlAltDeleteButtons.Subscribe(list => { Log.Information("CtrlAltDelete happened!"); });

            this.Reset = pressedCtrlAltDeleteButtons;
        }

        public override IObservable<ButtonPressedEventArgs> OnButton { get; }

        public override IObservable<ButtonIdentifier> OnButtonUp { get; }

        public override IObservable<ButtonIdentifier> OnButtonDown { get; }

        public override IObservable<IEnumerable<ButtonPressedEventArgs>> Reset { get; }

        public override IObservable<bool> IdleTimer { get; set; }

        public override IEnumerable<ILightableButton> LedButtonPinPins { get; }

        public override ILightableButton this[ButtonIdentifier buttonIdentifier]
        {
            get { return this.LedButtonPinPins.FirstOrDefault(lbpp => lbpp.ButtonIdentifier.Equals(buttonIdentifier)); }
        }

        public override IObservable<ButtonIdentifier> WaitForNextButton(TimeSpan timeout)
        {
            var observable = this.LedButtonPinPins
                .Select(lbpp => lbpp.ButtonDown)
                .Merge()
                .Timeout(timeout);

            return observable;
        }


        public override async Task Blink(IEnumerable<ButtonIdentifier> buttons, int times = 1, int duration = 200)
        {
            if (times < 1)
            {
                return;
            }

            var lightableButtons = this.GetLightableButtonsForIdentifiers(buttons);

            for (var i = 0; i < times; i++)
            {
                await Task.WhenAll(lightableButtons.Select(b => b.SetLight(true, duration)));

                if (i != times - 1)
                {
                    await Task.Delay(duration);
                }
            }
        }

        public override async Task BlinkAll(int times = 1, int duration = 200)
        {
            if (times < 1)
            {
                return;
            }

            for (var i = 0; i < times; i++)
            {
                await this.SetAll(true, duration);

                if (i != times - 1)
                {
                    await Task.Delay(duration);
                }
            }
        }

        public override IDisposable LightButtonOnPress()
        {
            var subscriptions = this.LedButtonPinPins
                .Select(lbpp => lbpp.Button.Subscribe(args => this.LightifyOnPress(lbpp, args)))
                .ToArray();

            return new CollectionDisposableWithFinalAction(() => this.SetAll(false), subscriptions);
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