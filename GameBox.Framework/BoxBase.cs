namespace GameBox.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Threading.Tasks;

    public abstract class BoxBase : IBox
    {
        public static readonly ButtonIdentifier GreenOneButtonIdentifier = new ButtonIdentifier(Player.One, Color.Green);

        public static readonly ButtonIdentifier YellowOneButtonIdentifier = new ButtonIdentifier(Player.One, Color.Yellow);

        public static readonly ButtonIdentifier RedOneButtonIdentifier = new ButtonIdentifier(Player.One, Color.Red);

        public static readonly ButtonIdentifier BlueOneButtonIdentifier = new ButtonIdentifier(Player.One, Color.Blue);

        public static readonly ButtonIdentifier WhiteOneButtonIdentifier = new ButtonIdentifier(Player.One, Color.White);

        public static readonly ButtonIdentifier GreenTwoButtonIdentifier = new ButtonIdentifier(Player.Two, Color.Green);

        public static readonly ButtonIdentifier YellowTwoButtonIdentifier = new ButtonIdentifier(Player.Two, Color.Yellow);

        public static readonly ButtonIdentifier RedTwoButtonIdentifier = new ButtonIdentifier(Player.Two, Color.Red);

        public static readonly ButtonIdentifier BlueTwoButtonIdentifier = new ButtonIdentifier(Player.Two, Color.Blue);

        public static readonly ButtonIdentifier WhiteTwoButtonIdentifier = new ButtonIdentifier(Player.Two, Color.White);

        protected BoxBase(BoxBaseOptions options)
        {
            this.Options = options;
        }

        public BoxBaseOptions Options { get; }

        public abstract IObservable<ButtonPressedEventArgs> OnButton { get; }
        
        public abstract IObservable<ResetArgs> Reset { get; }

        public abstract IObservable<ButtonIdentifier> OnButtonUp { get; }

        public abstract IObservable<ButtonIdentifier> OnButtonDown { get; }

        public abstract IObservable<bool> IdleTimer { get; set; }

        public abstract IEnumerable<ILightableButton> LedButtonPinPins { get; }

        public abstract ILightableButton this[ButtonIdentifier buttonIdentifier] { get; }

        public abstract IObservable<ButtonIdentifier> WaitForNextButton(TimeSpan timeout);

        public abstract Task Blink(IEnumerable<ButtonIdentifier> buttons, int times = 1, TimeSpan? duration = null);

        public abstract Task BlinkAll(int times = 1, TimeSpan? duration = null);

        public abstract IDisposable LightButtonOnPress();

        public abstract Task SetAll(bool enabled, TimeSpan? duration = null);

        public abstract Task Set(ButtonIdentifier button, bool enabled, TimeSpan? duration = null);

        public abstract Task Set(IEnumerable<ButtonIdentifier> buttons, bool enabled, TimeSpan? duration = null);
    }
}