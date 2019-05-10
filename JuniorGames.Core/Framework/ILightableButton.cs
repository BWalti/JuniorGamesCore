namespace JuniorGames.Core.Framework
{
    using System;
    using System.Drawing;
    using System.Threading.Tasks;

    public interface ILightableButton : IDisposable
    {
        ButtonIdentifier ButtonIdentifier { get; }

        Color Color { get; }

        Player Player { get; }

        IObservable<ButtonIdentifier> ButtonDown { get; }

        IObservable<ButtonIdentifier> ButtonUp { get; }

        IObservable<ButtonPressedEventArgs> Button { get; }

        Task SetLight(bool enabled, int? milliseconds = null);
    }
}