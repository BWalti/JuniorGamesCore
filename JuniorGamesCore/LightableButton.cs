namespace JuniorGames
{
    using System;
    using System.Device.Gpio;
    using System.Drawing;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Serilog;

    public class LightableButton : ILightableButton
    {
        private readonly GpioController gpioController;
        private bool isDisposed;


        public LightableButton(GpioController gpioController, LightableButtonConfig config)
        {
            Log.Information($"Creating LightableButton for {config.Identifier.Color} {config.Identifier.Player}");
            
            this.gpioController = gpioController;
            this.Config = config;

            this.CreateObservableButtons();
        }

        public LightableButtonConfig Config { get; }

        public ButtonIdentifier ButtonIdentifier => this.Config.Identifier;

        public Color Color => this.ButtonIdentifier.Color;
        public Player Player => this.ButtonIdentifier.Player;

        public IObservable<ButtonIdentifier> ButtonDown { get; private set; }

        public IObservable<ButtonIdentifier> ButtonUp { get; private set; }
        public IObservable<ButtonPressedEventArgs> Button { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
        }

        private event Action<ButtonIdentifier> OnButtonDown;

        private event Action<ButtonIdentifier> OnButtonUp;

        private void CreateObservableButtons()
        {
            Log.Information("Registering button callback...");
            this.gpioController.RegisterCallbackForPinValueChangedEvent(this.Config.ButtonPin, PinEventTypes.Rising | PinEventTypes.Falling, this.OnButtonChanged);

            var downSource = Observable.FromEvent<ButtonIdentifier>(
                action => this.OnButtonDown += action,
                action => this.OnButtonDown -= action);
            var upSource = Observable.FromEvent<ButtonIdentifier>(
                action => this.OnButtonUp += action,
                action => this.OnButtonUp -= action);

            this.ButtonDown = downSource.Throttle(TimeSpan.FromMilliseconds(10));
            this.ButtonUp = upSource.Throttle(TimeSpan.FromMilliseconds(10));
            this.Button = downSource
                .Select(d => new ButtonPressedEventArgs(d, true))
                .Merge(upSource
                    .Select(d => new ButtonPressedEventArgs(d, false)))
                .Throttle(TimeSpan.FromMilliseconds(10));
        }

        private void OnButtonChanged(object sender, PinValueChangedEventArgs args)
        {
            Log.Information($"Gpio Changed: {args.PinNumber} = {args.ChangeType}");
            if (args.PinNumber != this.Config.ButtonPin)
            {
                return;
            }

            switch (args.ChangeType)
            {
                case PinEventTypes.Falling:
                    this.OnButtonUp?.Invoke(this.ButtonIdentifier);
                    break;

                case PinEventTypes.Rising:
                    this.OnButtonDown?.Invoke(this.ButtonIdentifier);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        protected virtual void Dispose(bool isDisposing)
        {
            Log.Information($"Disposing Lightable Button: {this.Config.Identifier.Color} {this.Config.Identifier.Player}");
            if (isDisposing && !this.isDisposed)
            {
                this.isDisposed = true;
                this.gpioController.UnregisterCallbackForPinValueChangedEvent(this.Config.ButtonPin, this.OnButtonChanged);
            }
        }

        private PinValue oldValue = PinValue.Low;

        public async Task SetLight(bool enabled, int? milliseconds = null)
        {
            //var oldValue = this.gpioController.Read(this.Config.LedPin);

            var newValue = enabled ? PinValue.High : PinValue.Low;
            this.gpioController.Write(this.Config.LedPin, newValue);

            if (milliseconds.HasValue)
            {
                await Task.Delay(milliseconds.Value);
                this.gpioController.Write(this.Config.LedPin, this.oldValue);
            }
            else
            {
                this.oldValue = newValue;
            }
        }
    }
}