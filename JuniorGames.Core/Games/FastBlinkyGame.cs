namespace JuniorGames.Core.Games
{
    using System.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;

    /// <summary>
    ///     Experimental game: wanted to know, if it would be possible to "dim" the LED by letting it blink very fast.
    ///     Did not work out as I was hoping.
    /// </summary>
    public class FastBlinkyGame : GameBase
    {
        public FastBlinkyGame(IGameBox box) : base(box)
        {
        }

        protected override async Task OnStart()
        {
            var light = this.GameBox.LedButtonPinPins.First(l =>
                l.ButtonIdentifier.Equals(GameBoxBase.GreenOneButtonIdentifier));

            for (var i = 1; i < 1000; i++)
            {
                for (var j = 0; j < 1000 / i; j++)
                {
                    await light.SetLight(true, 2 * i);
                    await Task.Delay(i);
                }

                await Task.Delay(1000);
            }
        }
    }
}