namespace JuniorGames.Games
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using GameBox.Framework;

    /// <summary>
    ///     Experimental game: wanted to know, if it would be possible to "dim" the LED by letting it blink very fast.
    ///     Did not work out as I was hoping.
    /// </summary>
    public class FastBlinkyGame : GameBase
    {
        public FastBlinkyGame(IBox box) : base(box)
        {
        }

        protected override async Task OnStart()
        {
            var light = this.Box.LedButtonPinPins.First(l =>
                l.ButtonIdentifier.Equals(BoxBase.GreenOneButtonIdentifier));

            for (var i = 1; i < 1000; i++)
            {
                for (var j = 0; j < 1000 / i; j++)
                {
                    await light.SetLight(true, TimeSpan.FromMilliseconds(2 * i));
                    await Task.Delay(i);
                }

                await Task.Delay(1000);
            }
        }
    }
}