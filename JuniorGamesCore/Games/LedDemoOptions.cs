namespace JuniorGames.Core.Games
{
    public class LedDemoOptions : IOptions
    {
        public LedDemoOptions()
        {
            this.LightMs = 200;
            this.DarkMs = 200;
        }

        public int LightMs { get; set; }

        public int DarkMs { get; set; }
    }
}