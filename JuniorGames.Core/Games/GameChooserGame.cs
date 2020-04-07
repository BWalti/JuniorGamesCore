namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Serilog;
    using Stateless;

    /// <summary>
    ///     Not really a game, just providing some kind of "menu" to choose (and start) a game.
    /// </summary>
    public class GameChooserGame : GameBase
    {
        private readonly GameBootstrapper gameBootstrapper;
        private readonly IDisposable idleSubscription;
        private readonly TimeSpan idleTimeout;
        private readonly TimeSpan maximumGameTime;
        private readonly Dictionary<ButtonIdentifier, Func<IGame>> registeredGames;
        private readonly StateMachine<GameChooserState, GameChooserEvent> stateMachine;

        private readonly TaskCompletionSource<object> taskCompletionSource;
        private IGame game;
        private IDisposable temporarySubscription;
        private readonly object lockObj = new object();
        private StateMachine<GameChooserState, GameChooserEvent>.TriggerWithParameters<ButtonIdentifier> buttonPressedWithParameter;

        public GameChooserGame(GameBootstrapper gameBootstrapper, IGameBox gameBox, GameChooserOptions options) :
            base(gameBox)
        {
            this.stateMachine = this.ConfigureStateMachine();
            this.gameBootstrapper = gameBootstrapper;

            this.taskCompletionSource = new TaskCompletionSource<object>();
            this.maximumGameTime = TimeSpan.FromMinutes(options.MaximumGameTimeMinutes);
            this.idleTimeout = TimeSpan.FromSeconds(this.GameBox.Options.IdleTimeout);

            this.idleSubscription = this.GameBox.Reset
                .Select(buttons => true)
                .Merge(this.GameBox.IdleTimer)
                .Subscribe(next => this.SetIdle(null));

            this.registeredGames = new Dictionary<ButtonIdentifier, Func<IGame>>
            {
                {GameBoxBase.GreenOneButtonIdentifier, () => this.gameBootstrapper.AfterButner()},
                {GameBoxBase.YellowOneButtonIdentifier, () => this.gameBootstrapper.LightifyOnButtonPress()},
                {GameBoxBase.RedOneButtonIdentifier, () => this.gameBootstrapper.ChainGame(5)},
                //{GameBoxBase.BlueOneButtonIdentifier, () => this.gameBootstrapper.HueGame()}
            };
        }

        protected override async Task Start()
        {
            await this.stateMachine.FireAsync(GameChooserEvent.ButtonPressed);

            await this.taskCompletionSource.Task;
        }

        private async Task Sleep()
        {
            this.CleanRunningGame();

            await this.GameBox.SetAll(false);

            this.temporarySubscription = this.GameBox.LedButtonPinPins
                .Select(lbpp => lbpp.Button)
                .Merge()
                .Subscribe(this.WakeUpButtonPressed);
        }

        private void CleanRunningGame()
        {
            lock (this.lockObj)
            {
                if (this.game != null)
                {
                    this.game.Stop();
                    this.game.Dispose();

                    this.game = null;
                }
            }
        }

        private async void WakeUpButtonPressed(ButtonPressedEventArgs args)
        {
            this.CleanRunningGame();

            this.temporarySubscription.Dispose();
            await this.stateMachine.FireAsync(GameChooserEvent.ButtonPressed);
        }

        private async Task CreateAndStartGame(ButtonIdentifier button)
        {
            this.CleanRunningGame();

            if (this.registeredGames.TryGetValue(button, out var gameFactory))
            {
                this.game = gameFactory();
            }

            await this.StartGame();
        }

        private async Task WakeUp()
        {
            this.CleanRunningGame();

            Log.Information("WakeUp called");

            using (var ledDemo = this.gameBootstrapper.LedDemo())
            {
                await ledDemo.Start(this.maximumGameTime);
            }

            var allTasks = this.registeredGames.Keys
                .Select(bi => this.GameBox[bi])
                .Select(lbpp => lbpp.SetLight(true));

            await Task.WhenAll(allTasks);

            Log.Information("Awaiting next button");
            this.temporarySubscription = this.GameBox
                .WaitForNextButton(this.idleTimeout)
                .Subscribe(this.GameChosen, this.SetIdle);
        }

        private async void SetIdle(Exception obj)
        {
            this.CleanRunningGame();

            await this.stateMachine.FireAsync(GameChooserEvent.Idle);
        }

        private async void GameChosen(ButtonIdentifier buttonPressed)
        {
            Log.Information($"Game chosen: {buttonPressed.Player} / {buttonPressed.Color}");

            this.temporarySubscription.Dispose();

            await this.GameBox.SetAll(false);
            await this.stateMachine.FireAsync(this.buttonPressedWithParameter, buttonPressed);
        }

        private StateMachine<GameChooserState, GameChooserEvent> ConfigureStateMachine()
        {
            var fsm = new StateMachine<GameChooserState, GameChooserEvent>(GameChooserState.StandBy);
            fsm.Configure(GameChooserState.StandBy)
                .Permit(GameChooserEvent.ButtonPressed, GameChooserState.Awake);

            fsm.Configure(GameChooserState.Awake)
                .OnEntryAsync(this.WakeUp)
                .Permit(GameChooserEvent.ButtonPressed, GameChooserState.Game)
                .Permit(GameChooserEvent.Idle, GameChooserState.StandBy);

            this.buttonPressedWithParameter = fsm.SetTriggerParameters<ButtonIdentifier>(GameChooserEvent.ButtonPressed);
            fsm.Configure(GameChooserState.Game)
                .OnEntryFromAsync(buttonPressedWithParameter, this.CreateAndStartGame)
                .Permit(GameChooserEvent.Reset, GameChooserState.Awake)
                .Permit(GameChooserEvent.GameFinished, GameChooserState.Awake)
                .Permit(GameChooserEvent.Idle, GameChooserState.StandBy);

            return fsm;
        }

        private async Task StartGame()
        {
            if (this.game != null)
            {
                try
                {
                    await this.game.Start(this.maximumGameTime);
                }
                catch (Exception)
                {
                    await this.stateMachine.FireAsync(GameChooserEvent.Idle);
                }

                this.CleanRunningGame();
                await this.stateMachine.FireAsync(GameChooserEvent.GameFinished);
            }
            else
            {
                await this.stateMachine.FireAsync(GameChooserEvent.Reset);
            }
        }
    }

    public class GameChooserOptions : IOptions
    {
        public int MaximumGameTimeMinutes { get; set; }
    }
}