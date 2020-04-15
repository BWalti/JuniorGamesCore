namespace JuniorGames.Games
{
    using System;
    using GameBox.Framework;

    public class LedDemoOptions : IOptions
    {
        public LedDemoOptions()
        {
            this.LightUp = TimeSpan.FromMilliseconds(200);
            this.DarkMs = 200;
        }

        public TimeSpan LightUp { get; set; }

        public int DarkMs { get; set; }
    }
}