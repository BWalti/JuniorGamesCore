namespace GameBox.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IBox
    {
        BoxBaseOptions Options { get; }

        IObservable<ButtonPressedEventArgs> OnButton { get; }

        IObservable<ResetArgs> Reset { get; }

        IObservable<ButtonIdentifier> OnButtonUp { get; }

        IObservable<ButtonIdentifier> OnButtonDown { get; }

        IObservable<bool> IdleTimer { get; set; }

        IEnumerable<ILightableButton> LedButtonPinPins { get; }

        ILightableButton this[ButtonIdentifier buttonIdentifier] { get; }

        IObservable<ButtonIdentifier> WaitForNextButton(TimeSpan timeout);

        Task Blink(IEnumerable<ButtonIdentifier> buttons, int times = 1, TimeSpan? duration = null);

        Task BlinkAll(int times = 1, TimeSpan? duration = null);

        IDisposable LightButtonOnPress();

        Task SetAll(bool enabled, TimeSpan? duration = null);

        Task Set(ButtonIdentifier button, bool enabled, TimeSpan? duration = null);

        Task Set(IEnumerable<ButtonIdentifier> buttons, bool enabled, TimeSpan? duration = null);
    }
}