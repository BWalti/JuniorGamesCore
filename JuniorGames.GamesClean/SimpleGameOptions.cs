namespace JuniorGames.GamesClean
{
    using System;

    public class SimpleGameOptions
    {
        public SimpleGameOptions()
        {
            this.Pause = TimeSpan.FromMilliseconds(300);
            this.LightUp = TimeSpan.FromMilliseconds(400);
            this.Retries = 3;
            this.StartLength = 3;
            this.MaxChainLength = 20;
            this.MaxSpeedFactor = 2;
        }

        public TimeSpan Pause { get; set; }
        public TimeSpan LightUp { get; set; }
        public int Retries { get; set; }
        public int MaxChainLength { get; set; }

        /// <summary>
        /// Defines how fast the chain gets displayed when at <see cref="MaxChainLength"/>.
        /// </summary>
        /// <remarks>
        /// A speed factor of 2 meaning <see cref="Pause"/> as well as <see cref="LightUp"/>
        /// will be cut in half for the whole chain.
        /// </remarks>
        public double MaxSpeedFactor { get; set; }

        public int StartLength { get; set; }
    }
}