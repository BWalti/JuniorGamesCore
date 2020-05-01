namespace JuniorGames.GamesClean
{
    using System;

    public class SimpleGameTimeSpanCalculator
    {
        private readonly SimpleGameOptions options;
        private readonly SimpleGameStatus status;

        public SimpleGameTimeSpanCalculator(SimpleGameOptions options, SimpleGameStatus status)
        {
            this.options = options;
            this.status = status;
        }

        public TimeSpan GetCurrentPauseDuration()
        {
            var timeSpan = this.options.Pause;
            return this.CalculateCurrentTimeSpan(timeSpan);
        }

        public TimeSpan GetCurrentLightUpDuration()
        {
            var timeSpan = this.options.LightUp;
            return this.CalculateCurrentTimeSpan(timeSpan);
        }

        private TimeSpan CalculateCurrentTimeSpan(TimeSpan timeSpan)
        {
            var steps = this.options.MaxChainLength - this.options.StartLength;
            var currentStep = this.status.Chain.Count - this.options.StartLength;
            var progress = currentStep / (double) steps;
            var speedFactorMaxIncrement = this.options.MaxSpeedFactor - 1;
            var speedFactor = 1 + progress * speedFactorMaxIncrement;
            var totalMillis = timeSpan.TotalMilliseconds / speedFactor;

            return TimeSpan.FromMilliseconds(totalMillis);
        }
    }
}