namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;

    public class TwoPlayersGame : GameBase
    {
        private readonly TwoPlayersGameOptions options;
        private List<Color> randomColors;
        private int indexOne;
        private int indexTwo;

        private List<ButtonIdentifier> playerOneButtons;

        private List<ButtonIdentifier> playerTwoButtons;

        private TaskCompletionSource<object> taskCompletionSource;

        private IDisposable buttonDownSubscription;

        private bool ignoreOnes;
        private bool ignoreTwos;

        private int games;

        public TwoPlayersGame(IGameBox box, TwoPlayersGameOptions options) : base(box)
        {
            this.options = options;
        }

        protected override async Task OnStart()
        {
            this.Init();
            this.InitializeChain();

            await this.Play();

            await this.taskCompletionSource.Task;
        }

        private async Task Play()
        {
            var buttonOne = this.GetButton(this.playerOneButtons, this.indexOne);
            var buttonTwo = this.GetButton(this.playerTwoButtons, this.indexTwo);

            await Task.WhenAll(this.GameBox.Set(buttonOne, true), this.GameBox.Set(buttonTwo, true));

            //this.buttonDownSubscription?.Dispose();

            this.buttonDownSubscription = this.GameBox.OnButtonDown.Subscribe(this.ButtonDown);
        }

        private void ButtonDown(ButtonIdentifier buttonIdentifier)
        {
            switch (buttonIdentifier.Player)
            {
                case Player.One:
                    this.PlayerOneButtonDown(buttonIdentifier);
                    break;
                case Player.Two:
                    this.PlayerTwoButtonDown(buttonIdentifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async void PlayerTwoButtonDown(ButtonIdentifier buttonIdentifier)
        {
            if (this.ignoreTwos)
            {
                return;
            }

            this.ignoreTwos = true;
            this.indexTwo = await this.HandlePlayerButton(buttonIdentifier, this.indexTwo, this.playerTwoButtons);
            await this.ProceedOrGameFinished(this.indexTwo, this.playerTwoButtons);
            this.ignoreTwos = false;
        }

        private async Task ProceedOrGameFinished(int index, IEnumerable<ButtonIdentifier> buttonIdentifiers)
        {
            if (index >= this.randomColors.Count)
            {
                this.buttonDownSubscription?.Dispose();
                this.buttonDownSubscription = null;

                // yes, we are finished!
                await this.GameBox.Set(buttonIdentifiers, true);
                await Task.Delay(this.options.PauseBetweenGames);

                this.games++;
                if (this.games > this.options.Games)
                {
                    this.taskCompletionSource.SetResult(0);
                }
                else
                {
                    await this.GameBox.SetAll(false);
                    this.InitializeChain();
                    await this.Play();
                }
            }
            else
            {
                var bi = this.GetButton(buttonIdentifiers, index);

                await Task.Delay(this.options.PauseBetweenButtons);
                await this.GameBox.Set(bi, true);
            }
        }

        private async void PlayerOneButtonDown(ButtonIdentifier buttonIdentifier)
        {
            if (this.ignoreOnes)
            {
                return;
            }

            this.ignoreOnes = true;
            this.indexOne = await this.HandlePlayerButton(buttonIdentifier, this.indexOne, this.playerOneButtons);
            await this.ProceedOrGameFinished(this.indexOne, this.playerOneButtons);
            this.ignoreOnes = false;
        }

        private async Task<int> HandlePlayerButton(ButtonIdentifier buttonIdentifier, int index, IEnumerable<ButtonIdentifier> buttonIdentifiers)
        {
            var color = this.GetColor(index);
            if (buttonIdentifier.Color == color)
            {
                // correct! Thus turn light off, increase index and turn next light on:
                await this.GameBox.Set(buttonIdentifier, false);
                index++;
            }
            else
            {
                await this.GameBox.Blink(buttonIdentifiers, 2, 100);
            }

            return index;
        }

        private ButtonIdentifier GetButton(IEnumerable<ButtonIdentifier> buttonIdentifiers, int index)
        {
            return buttonIdentifiers.First(b => b.Color == this.GetColor(index));
        }

        private Color GetColor(int index)
        {
            return this.randomColors[index];
        }

        private void Init()
        {
            this.games = 0;

            var groups = this.GameBox.LedButtonPinPins
            .GroupBy(lbpp => lbpp.ButtonIdentifier.Player, lbpp => lbpp.ButtonIdentifier)
            .ToDictionary(g => g.Key, g => g.ToList());

            this.playerOneButtons = groups[Player.One];
            this.playerTwoButtons = groups[Player.Two];

            this.taskCompletionSource = new TaskCompletionSource<object>();
        }

        private void InitializeChain()
        {
            // idea: let the button light until pressed and then proceed enabling the next button
            var colorLookup = this.playerOneButtons
            .Select((bi, index) => Tuple.Create(index, bi))
            .ToDictionary(t => t.Item1, t => t.Item2.Color);

            this.indexOne = 0;
            this.indexTwo = 0;

            this.randomColors = new List<Color>();
            var lastRandom = -1;
            var rnd = new Random();

            for (var i = 0; i < this.options.ChainLength; i++)
            {
                var next = rnd.Next(0, this.playerOneButtons.Count);
                while (next == lastRandom)
                {
                    next = rnd.Next(0, this.playerOneButtons.Count);
                }

                lastRandom = next;

                this.randomColors.Add(colorLookup[next]);
            }
        }
    }

    public class TwoPlayersGameOptions : IOptions
    {
        public TwoPlayersGameOptions()
        {
            this.ChainLength = 20;
            this.Games = 3;
            this.PauseBetweenButtons = TimeSpan.FromMilliseconds(200);
            this.PauseBetweenGames = TimeSpan.FromSeconds(3);
        }

        public TimeSpan PauseBetweenButtons { get; set; }
        public int ChainLength { get; set; }
        public int Games { get; set; }

        public TimeSpan PauseBetweenGames { get; set; }
    }
}