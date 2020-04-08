namespace JuniorGames.Core.Games
{
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;

    /// <summary>
    ///     This Game automatically lights up each LED once, then all together twice.
    /// </summary>
    public class LedDemoGame : GameBase
    {
        private readonly LedDemoOptions options;

        public LedDemoGame(IGameBox gameBox, LedDemoOptions options) : base(gameBox)
        {
            this.options = options;
        }

        protected override async Task OnStart()
        {
            await this.GameBox.SetAll(false);

            foreach (var ledButtonPinPin in this.GameBox.LedButtonPinPins)
            {
                await ledButtonPinPin.SetLight(true, this.options.LightMs);
                await Task.Delay(this.options.DarkMs);
            }

            await Task.Delay(this.options.DarkMs);

            await this.GameBox.BlinkAll(2, this.options.LightMs);
        }
    }
}