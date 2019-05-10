namespace JuniorGames.Core.Games
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;

    /// <summary>
    ///     This game lights up the LED to the corresponding button as soon as the button has been pressed
    ///     and turns the LED off, 2 seconds after the last depress of that button.
    ///     (Meaning if you continously press the same button and time between pressing the same button is always less than two
    ///     seconds, it will never turn off)
    /// </summary>
    public class AfterBurnerGame : GameBase
    {
        private IDisposable subscription;

        public AfterBurnerGame(IGameBox box) : base(box)
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && (this.subscription != null))
            {
                this.subscription.Dispose();
                this.subscription = null;
            }
        }

        protected override async Task Start()
        {
            var turnOnSubscription = this.GameBox.OnButtonDown.Subscribe(this.LightUp);
            var turnOffSubscription = this.GameBox
                .LedButtonPinPins
                .Select(lbpp => lbpp.Button.Throttle(TimeSpan.FromSeconds(2))
                    .Where(a => !a.IsPressed)
                    .Select(a => a.Identifier)
                    .Subscribe(this.LightOut));

            var allSubscriptions = turnOffSubscription.Concat(new[] {turnOnSubscription}).ToArray();

            this.subscription = new CollectionDisposable(allSubscriptions);

            for (var i = 0; i < 6; i++)
            {
                this.CancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(10), this.CancellationToken);
            }
        }

        private async void LightOut(ButtonIdentifier args)
        {
            await this.GameBox.Set(args, false);
        }

        private async void LightUp(ButtonIdentifier args)
        {
            await this.GameBox.Set(args, true);
        }
    }
}