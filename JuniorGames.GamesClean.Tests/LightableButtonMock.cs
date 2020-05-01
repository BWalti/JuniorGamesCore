namespace JuniorGames.GamesClean.Tests
{
    using System;
    using System.Drawing;
    using System.Threading;
    using System.Threading.Tasks;
    using GameBox.Framework;

    public class LightableButtonMock : ILightableButton
    {
        public void Dispose()
        {
        }

        public ButtonIdentifier ButtonIdentifier { get; set; }
        public Color Color { get; set; }
        public Player Player { get; set; }
        public IObservable<ButtonIdentifier> ButtonDown { get; set; }
        public IObservable<ButtonIdentifier> ButtonUp { get; set; }
        public IObservable<ButtonPressedEventArgs> Button { get; set; }

        public Task SetLight(bool enabled, TimeSpan? duration = null)
        {
            return Task.CompletedTask;
        }
    }
}