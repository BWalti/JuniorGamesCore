namespace JuniorGames.Games
{
    using System.Threading.Tasks;
    using GameBox.Framework;

    /// <summary>
    ///     This Game automatically lights up each LED once, then all together twice.
    /// </summary>
    public class LedDemoGame : GameBase
    {
        private readonly LedDemoOptions options;

        public LedDemoGame(IBox box, LedDemoOptions options) : base(box)
        {
            this.options = options;
        }

        protected override async Task OnStart()
        {
            await this.Box.SetAll(false);

            foreach (var ledButtonPinPin in this.Box.LedButtonPinPins)
            {
                await ledButtonPinPin.SetLight(true, this.options.LightUp);
                await Task.Delay(this.options.DarkMs);
            }

            await Task.Delay(this.options.DarkMs);

            await this.Box.BlinkAll(2, this.options.LightUp);

            this.NotifyGameComplete();
        }
    }
}