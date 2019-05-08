namespace JuniorGames.Core.Framework
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IGameBox
    {
        GameBoxOptions Options { get; }

        IObservable<ButtonPressedEventArgs> OnButton { get; }

        IObservable<ButtonIdentifier> OnButtonUp { get; }

        IObservable<ButtonIdentifier> OnButtonDown { get; }

        IObservable<bool> IdleTimer { get; set; }

        IEnumerable<ILightableButton> LedButtonPinPins { get; }

        ILightableButton this[ButtonIdentifier buttonIdentifier] { get; }

        IObservable<ButtonIdentifier> WaitForNextButton(TimeSpan timeout);

        Task Blink(IEnumerable<ButtonIdentifier> buttons, int times = 1, int duration = 200);

        Task BlinkAll(int times = 1, int duration = 200);

        IDisposable LightButtonOnPress();

        Task SetAll(bool enabled, int? duration = null);

        Task Set(ButtonIdentifier button, bool enabled, int? milliseconds = null);

        Task Set(IEnumerable<ButtonIdentifier> buttons, bool enabled, int? milliseconds = null);
    }
}