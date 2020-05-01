namespace JuniorGames.GamesClean.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using GameBox.Framework;
    using Xunit;

    public class SimpleGameTimeSpanCalculatorTests
    {
        public SimpleGameTimeSpanCalculatorTests()
        {
            this.chain = Enumerable.Range(0, 3).Select(_ => new LightableButtonMock() as ILightableButton).ToList();

            this.state = new SimpleGameStatus(this.chain);
            this.options = new SimpleGameOptions
            {
                LightUp = TimeSpan.FromMilliseconds(400),
                MaxChainLength = 13,
                MaxSpeedFactor = 2,
                Pause = TimeSpan.FromMilliseconds(200),
                Retries = 3,
                StartLength = 3
            };

            this.testee = new SimpleGameTimeSpanCalculator(this.options, this.state);
        }

        private readonly List<ILightableButton> chain;
        private SimpleGameStatus state;
        private SimpleGameOptions options;
        private SimpleGameTimeSpanCalculator testee;
        
        [Fact]
        public void GetCurrentLightUpDuration_WhenAtTheBeginning_ThenNotSpeedUp()
        {
            var actual = this.testee.GetCurrentLightUpDuration();
            Assert.Equal(this.options.LightUp, actual);
        }
        
        [Fact]
        public void GetCurrentPauseDuration_WhenAtTheBeginning_ThenNotSpeedUp()
        {
            var actual = this.testee.GetCurrentPauseDuration();
            Assert.Equal(this.options.Pause, actual);
        }

        [Fact]
        public void GetCurrentLightUpDuration_WhenAtTheEnd_ThenCompletelySpeedUp()
        {
            var additionalButtons = this.GenerateMockButtons(this.options.MaxChainLength - this.state.Chain.Count);
            this.state.Chain.AddRange(additionalButtons);

            var actual = this.testee.GetCurrentLightUpDuration();
            Assert.Equal(this.options.LightUp / this.options.MaxSpeedFactor, actual);
        }
        
        [Fact]
        public void GetCurrentPauseDuration_WhenAtTheEnd_ThenCompletelySpeedUp()
        {
            var additionalButtons = this.GenerateMockButtons(this.options.MaxChainLength - this.state.Chain.Count);
            this.state.Chain.AddRange(additionalButtons);

            var actual = this.testee.GetCurrentPauseDuration();
            Assert.Equal(this.options.Pause / this.options.MaxSpeedFactor, actual);
        }

        private List<ILightableButton> GenerateMockButtons(int count) => 
            Enumerable.Range(0, count).Select(_ => new LightableButtonMock() as ILightableButton).ToList();
    }
}