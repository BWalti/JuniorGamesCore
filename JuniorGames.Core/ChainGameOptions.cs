﻿namespace JuniorGames.Games
{
    using GameBox.Framework;

    public class ChainGameOptions : IOptions
    {
        public ChainGameOptions()
        {
            this.Games = 10;
            this.Retries = 3;
        }

        public int Games { get; set; }

        public int Retries { get; set; }
    }
}