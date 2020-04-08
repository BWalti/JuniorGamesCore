namespace JuniorGames.Core.Framework
{
    using System;

    public class GameBoxOptions : IOptions
    {
        public TimeSpan IdleTimeout { get; set; }
    }
}