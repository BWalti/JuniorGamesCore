namespace JuniorGames.Games
{
    using System;
    using System.Threading.Tasks;
    using GameBox.Framework;

    /// <summary>
    ///     This game lights up the LED(s) corresponding to the button(s) pressed
    ///     and turns them off as soon as the corresponding button has been de-pressed.
    /// </summary>
    public class LightifyOnPressGame : GameBase
    {
        private IDisposable subscription;

        public LightifyOnPressGame(IBox box) : base(box)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && (this.subscription != null))
            {
                this.subscription.Dispose();
                this.subscription = null;
            }
        }

        protected override async Task OnStart()
        {
            this.subscription = this.Box.LightButtonOnPress();

            for (var i = 0; i < 6; i++)
            {
                this.CancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(10), this.CancellationToken);
            }
        }
    }
}