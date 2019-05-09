namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using Appccelerate.StateMachine;
    using Appccelerate.StateMachine.AsyncMachine;
    using JuniorGames.Core.Framework;
    using Serilog;

    /// <summary>
    ///     Not really a game, just providing some kind of "menu" to choose (and start) a game.
    /// </summary>
    public class GameChooserGame : GameBase
    {
        private readonly GameBootstrapper gameBootstrapper;
        private readonly IDisposable idleSubscription;
        private readonly TimeSpan idleTimeout;
        private readonly TimeSpan maximumGameTime;
        private readonly AsyncPassiveStateMachine<GameChooserState, GameChooserEvent> stateMachine;
        private readonly Dictionary<ButtonIdentifier, Func<IGame>> registeredGames;

        private readonly TaskCompletionSource<object> taskCompletionSource;
        private IGame game;
        private IDisposable temporarySubscription;
        private Dictionary<ButtonIdentifier, Func<IGame>> gameRegistration;

        public GameChooserGame(GameBootstrapper gameBootstrapper, IGameBox gameBox, GameChooserOptions options) :
            base(gameBox)
        {
            this.stateMachine = this.ConfigureStateMachine();
            this.gameBootstrapper = gameBootstrapper;

            this.taskCompletionSource = new TaskCompletionSource<object>();
            this.maximumGameTime = TimeSpan.FromMinutes(options.MaximumGameTimeMinutes);
            this.idleTimeout = TimeSpan.FromSeconds(this.GameBox.Options.IdleTimeout);

            this.idleSubscription = this.GameBox.IdleTimer.Subscribe(next => this.SetIdle(null));

            this.registeredGames = new Dictionary<ButtonIdentifier, Func<IGame>>
            {
                { GameBoxBase.GreenOneButtonIdentifier, () => this.gameBootstrapper.AfterButner() },
                { GameBoxBase.YellowOneButtonIdentifier, () => this.gameBootstrapper.LightifyOnButtonPress() },
                { GameBoxBase.RedOneButtonIdentifier, () => this.gameBootstrapper.ChainGame(5) },
                { GameBoxBase.BlueOneButtonIdentifier, () => this.gameBootstrapper.HueGame() },
            };
        }

        protected override async Task Start()
        {
            this.stateMachine.Initialize(GameChooserState.StandBy);
            await this.stateMachine.Start();
            await this.stateMachine.Fire(GameChooserEvent.ButtonPressed);

            await this.taskCompletionSource.Task;
        }

        private async Task Sleep()
        {
            if (this.game != null)
            {
                this.game.Stop();
                this.game.Dispose();

                this.game = null;
            }

            await this.GameBox.SetAll(false);

            this.temporarySubscription = this.GameBox.LedButtonPinPins
                .Select(lbpp => lbpp.Button)
                .Merge()
                .Subscribe(this.WakeUpButtonPressed);
        }

        private async void WakeUpButtonPressed(ButtonPressedEventArgs args)
        {
            this.temporarySubscription.Dispose();
            await this.stateMachine.Fire(GameChooserEvent.ButtonPressed);
        }

        private Task CreateGame(ButtonIdentifier button)
        {
            this.game = null;
            if (this.registeredGames.TryGetValue(button, out var gameFactory))
            {
                this.game = gameFactory();
            }

            return Task.CompletedTask;
        }

        private void RegisterGames()
        {
            this.gameRegistration = new Dictionary<ButtonIdentifier, Func<IGame>>
            {
                {GameBoxBase.GreenOneButtonIdentifier, () => this.gameBootstrapper.AfterButner()},
                {GameBoxBase.YellowOneButtonIdentifier, () => this.gameBootstrapper.LightifyOnButtonPress()},
                {GameBoxBase.RedOneButtonIdentifier, () => this.gameBootstrapper.ChainGame(5)},
                {GameBoxBase.BlueOneButtonIdentifier, () => this.gameBootstrapper.TwoPlayers()}
            };
        }

        private async Task WakeUp()
        {
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
            if (this.game != null)
            {
                this.game.Stop();
                this.game.Dispose();

                this.game = null;
            }

            await this.stateMachine.Fire(GameChooserEvent.Idle);
        }

        private async void GameChosen(ButtonIdentifier buttonPressed)
        {
            Log.Information($"Game chosen: {buttonPressed.Player} / {buttonPressed.Color}");

            this.temporarySubscription.Dispose();

            await this.GameBox.SetAll(false);
            await this.stateMachine.Fire(GameChooserEvent.ButtonPressed, buttonPressed);
        }

        private AsyncPassiveStateMachine<GameChooserState, GameChooserEvent> ConfigureStateMachine()
        {
            var fsm = new AsyncPassiveStateMachine<GameChooserState, GameChooserEvent>();
            fsm.In(GameChooserState.StandBy)
                .On(GameChooserEvent.ButtonPressed)
                .Goto(GameChooserState.Awake)
                .Execute(this.WakeUp)
                .On(GameChooserEvent.GameFinished)
                .Goto(GameChooserState.StandBy);

            fsm.In(GameChooserState.Awake)
                .On(GameChooserEvent.ButtonPressed)
                .Goto(GameChooserState.Game)
                .Execute<ButtonIdentifier>(this.CreateGame)
                .On(GameChooserEvent.Idle)
                .Goto(GameChooserState.StandBy)
                .Execute(this.Sleep);

            fsm.In(GameChooserState.Game)
                .ExecuteOnEntry(this.StartGame)
                .On(GameChooserEvent.Reset)
                .Goto(GameChooserState.Awake)
                .Execute(this.WakeUp)
                .On(GameChooserEvent.GameFinished)
                .Goto(GameChooserState.Awake)
                .Execute(this.WakeUp)
                .On(GameChooserEvent.Idle)
                .Goto(GameChooserState.StandBy)
                .Execute(this.Sleep);

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
                catch (StateMachineException ex)
                {
                    if (ex.InnerException is OperationCanceledException)
                    {
                        // timeout
                        await this.stateMachine.Fire(GameChooserEvent.Idle);
                        return;
                    }
                }
                catch (TaskCanceledException)
                {
                }

                this.game.Dispose();
                await this.stateMachine.Fire(GameChooserEvent.GameFinished);
            }
            else
            {
                await this.stateMachine.Fire(GameChooserEvent.Reset);
            }
        }
    }

    public class GameChooserOptions : IOptions
    {
        public int MaximumGameTimeMinutes { get; set; }
    }
}