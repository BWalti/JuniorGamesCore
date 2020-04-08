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
        private readonly TimeSpan idleTimeout;
        private readonly object lockObj = new object();
        private readonly TimeSpan maximumGameTime;
        private readonly Dictionary<ButtonIdentifier, Func<IGame>> registeredGames;
        private readonly StateMachine<GameChooserState, GameChooserEvent> stateMachine;

        private StateMachine<GameChooserState, GameChooserEvent>.TriggerWithParameters<ButtonIdentifier>
            buttonPressedWithParameter;

        private IGame game;
        private IDisposable resetSubscription;
        private IDisposable wakeupOrIdleSubscription;

        public GameChooserGame(GameBootstrapper gameBootstrapper, IGameBox gameBox, GameChooserOptions options) :
            base(gameBox)
        {
            this.stateMachine = this.ConfigureStateMachine();
            this.gameBootstrapper = gameBootstrapper;

            this.maximumGameTime = TimeSpan.FromMinutes(options.MaximumGameTimeMinutes);
            this.idleTimeout = this.GameBox.Options.IdleTimeout;

            this.resetSubscription = this.GameBox.Reset
                .Select(buttons => true)
                .Merge(this.GameBox.IdleTimer)
                .Subscribe(next => this.FireReset());

            this.registeredGames = new Dictionary<ButtonIdentifier, Func<IGame>>
            {
                {GameBoxBase.GreenOneButtonIdentifier, () => this.gameBootstrapper.AfterButner()},
                {GameBoxBase.YellowOneButtonIdentifier, () => this.gameBootstrapper.LightifyOnButtonPress()},
                {GameBoxBase.RedOneButtonIdentifier, () => this.gameBootstrapper.ChainGame(5)}

                //{GameBoxBase.BlueOneButtonIdentifier, () => this.gameBootstrapper.HueGame()}
            };
        }

        private async void FireReset()
        {
            Log.Information("Firing reset");
            await this.stateMachine.FireAsync(GameChooserEvent.Reset);
        }

        protected override async Task OnStart()
        {
            Log.Information("Firing any ButtonPressed");
            await this.stateMachine.FireAsync(GameChooserEvent.ButtonPressed);
        }

        private async Task Sleep()
        {
            this.CleanRunningGame();

            await this.GameBox.SetAll(false);

            this.wakeupOrIdleSubscription = this.GameBox.LedButtonPinPins
                .Select(lbpp => lbpp.Button)
                .Merge()
                .Subscribe(this.FireButtonPressed);
        }

        private void CleanRunningGame()
        {
            lock (this.lockObj)
            {
                if (this.game != null)
                {
                    Log.Information("Cleaning game...");
                    this.game.Stop();
                    this.game.Dispose();

                    this.game = null;
                }
            }
        }

        private async void FireButtonPressed(ButtonPressedEventArgs args)
        {
            this.wakeupOrIdleSubscription.Dispose();
            Log.Information($"Firing button {args.Identifier} pressed");
            await this.stateMachine.FireAsync(GameChooserEvent.ButtonPressed);
        }

        private async Task CreateAndStartGame(ButtonIdentifier button)
        {
            this.CleanRunningGame();

            if (this.registeredGames.TryGetValue(button, out var gameFactory))
            {
                Log.Information("Creating game...");
                this.game = gameFactory();
            }

            await this.StartGame();
        }

        private async Task WakeUp()
        {
            Log.Information("Waking up...");
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
            this.wakeupOrIdleSubscription = this.GameBox
                .WaitForNextButton(this.idleTimeout)
                .Subscribe(this.ChooseGame, this.FireIdle);
        }

        private async void FireIdle(Exception obj)
        {
            Log.Information("Firing Idle");
            await this.stateMachine.FireAsync(GameChooserEvent.Idle);
        }

        private async void ChooseGame(ButtonIdentifier buttonPressed)
        {
            Log.Information($"Game chosen: {buttonPressed.Player} / {buttonPressed.Color}");

            this.wakeupOrIdleSubscription.Dispose();

            await this.GameBox.SetAll(false);
            Log.Information("Firing button pressed");
            await this.stateMachine.FireAsync(this.buttonPressedWithParameter, buttonPressed);
        }

        private StateMachine<GameChooserState, GameChooserEvent> ConfigureStateMachine()
        {
            var fsm = new StateMachine<GameChooserState, GameChooserEvent>(GameChooserState.StandBy);
            fsm.Configure(GameChooserState.StandBy)
                .OnEntryAsync(this.Sleep)
                .Permit(GameChooserEvent.ButtonPressed, GameChooserState.Awake)
                .PermitReentry(GameChooserEvent.Reset);
                
            fsm.Configure(GameChooserState.Awake)
                .OnEntryAsync(this.WakeUp)
                .Permit(GameChooserEvent.ButtonPressed, GameChooserState.Game)
                .Permit(GameChooserEvent.Idle, GameChooserState.StandBy)
                .PermitReentry(GameChooserEvent.Reset);

            this.buttonPressedWithParameter =
                fsm.SetTriggerParameters<ButtonIdentifier>(GameChooserEvent.ButtonPressed);
            fsm.Configure(GameChooserState.Game)
                .OnEntryFromAsync(this.buttonPressedWithParameter, this.CreateAndStartGame)
                .Permit(GameChooserEvent.Reset, GameChooserState.Awake)
                .Permit(GameChooserEvent.GameFinished, GameChooserState.Awake)
                .Permit(GameChooserEvent.Idle, GameChooserState.StandBy)
                .Permit(GameChooserEvent.Reset, GameChooserState.Awake);

            return fsm;
        }

        protected override void Dispose(bool disposing)
        {
            Log.Information("Disposing...");
            this.CleanRunningGame();

            base.Dispose(disposing);
            this.stateMachine.Deactivate();
            this.resetSubscription.Dispose();
        }

        private async Task StartGame()
        {
            if (this.game != null)
            {
                try
                {
                    Log.Information("Starting game...");
                    await this.game.Start(this.maximumGameTime);

                    Log.Information("Game finished...");
                    await this.stateMachine.FireAsync(GameChooserEvent.GameFinished);
                }
                catch (Exception e)
                {
                    Log.Information($"Exception from game: {e.Message}");
                    await this.stateMachine.FireAsync(GameChooserEvent.Idle);
                }
            }
            else
            {
                Log.Information("Resetting...");
                await this.stateMachine.FireAsync(GameChooserEvent.Reset);
            }
        }
    }

    public class GameChooserOptions : IOptions
    {
        public int MaximumGameTimeMinutes { get; set; }
    }
}