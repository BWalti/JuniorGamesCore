namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Appccelerate.StateMachine;
    using Appccelerate.StateMachine.AsyncMachine.Events;
    using JuniorGames.Core.Framework;
    using Newtonsoft.Json;
    using Polly;
    using Q42.HueApi;
    using Q42.HueApi.Interfaces;
    using Q42.HueApi.Models.Bridge;
    using Serilog;

    public enum HueBridgeEvent
    {
        Register,
        Connected,
        Select,
        Unselect,
        Located
    }

    public enum HueBridgeState
    {
        Located, // pulse twice
        Register, // pulse trice
        Connected, // blink all the time
        Selected // permanent on
        ,
        Initialized
    }

    public class HueStateMachine : IDisposable
    {
        private readonly LocatedBridge bridge;
        private readonly ILightableButton button;
        private readonly CancellationToken cancellationToken;
        private readonly IGameBox gameBox;
        private readonly List<ILightableButton> playerTwoButtons;
        private readonly IAsyncStateMachine<HueBridgeState, HueBridgeEvent> stateMachine;
        private Task blinkTask;
        private LocalHueClient client;
        private IEnumerable<HueLightButton> hueLightButtons;
        private bool notConnected;

        public HueStateMachine(IGameBox gameBox,
            LocatedBridge bridge,
            ILightableButton button,
            List<ILightableButton> playerTwoButtons,
            CancellationToken cancellationToken)
        {
            this.gameBox = gameBox;
            this.bridge = bridge;
            this.button = button;
            this.playerTwoButtons = playerTwoButtons;
            this.cancellationToken = cancellationToken;

            this.stateMachine = new AsyncPassiveStateMachine<HueBridgeState, HueBridgeEvent>();

            this.stateMachine.In(HueBridgeState.Initialized)
                .On(HueBridgeEvent.Located)
                .Goto(HueBridgeState.Located);

            this.stateMachine.In(HueBridgeState.Located)
                .ExecuteOnEntry(this.Located)
                .On(HueBridgeEvent.Connected)
                .Goto(HueBridgeState.Connected)
                .On(HueBridgeEvent.Register)
                .Goto(HueBridgeState.Register);

            this.stateMachine.In(HueBridgeState.Register)
                .ExecuteOnEntry(this.Register)
                .On(HueBridgeEvent.Connected)
                .Goto(HueBridgeState.Connected);

            this.stateMachine.In(HueBridgeState.Connected)
                .ExecuteOnEntry(this.Connected)
                .On(HueBridgeEvent.Select)
                .Goto(HueBridgeState.Selected);

            this.stateMachine.In(HueBridgeState.Selected)
                .ExecuteOnEntry(this.Selected)
                .On(HueBridgeEvent.Unselect)
                .Goto(HueBridgeState.Connected)
                .Execute(this.Unselect);

            this.stateMachine.TransitionExceptionThrown += this.StateMachineOnTransitionExceptionThrown;

            this.stateMachine.Initialize(HueBridgeState.Initialized);
        }

        private ButtonIdentifier[] Buttons => new[] {this.button.ButtonIdentifier};

        private string FileName => $"{this.bridge.IpAddress}-hue.json";

        public void Dispose()
        {
            this.notConnected = false;
            this.button?.Dispose();

            this.blinkTask.Wait(TimeSpan.FromSeconds(10));
            this.blinkTask?.Dispose();

            foreach (var hueLightButton in this.hueLightButtons)
            {
                hueLightButton.Dispose();
            }
        }

        public async Task DoUnselect()
        {
            await this.stateMachine.Fire(HueBridgeEvent.Unselect);
        }

        private void Unselect()
        {
            foreach (var hueLightButton in this.hueLightButtons)
            {
                hueLightButton.Dispose();
            }
        }

        private async Task Selected()
        {
            var lights = await this.client.GetLightsAsync();

            var reachableLights = lights.Where(l => l.State.IsReachable ?? false).ToList();

            this.hueLightButtons = reachableLights.Zip(this.playerTwoButtons,
                (light, lightableButton) => new HueLightButton(this.gameBox, this.client, light, lightableButton));

            var tasks = this.hueLightButtons.Select(hb => hb.Start());
            await Task.WhenAll(tasks);
        }

        private void StateMachineOnTransitionExceptionThrown(object sender,
            TransitionExceptionEventArgs<HueBridgeState, HueBridgeEvent> e)
        {
            Log.Error(e.Exception, "State Transition exception occured");
        }

        public async Task Start()
        {
            await this.stateMachine.Start();
            await this.stateMachine.Fire(HueBridgeEvent.Located);
        }

        private async Task Connected()
        {
            this.notConnected = false;

            await this.gameBox.Blink(this.Buttons, 2);
            await this.button.SetLight(true);

            this.button.ButtonUp.Subscribe(async identifier => await this.DoSelect());
        }

        private async Task DoSelect()
        {
            await this.stateMachine.Fire(HueBridgeEvent.Select);
            this.OnSelected?.Invoke();
        }

        public event Action OnSelected;

        private async Task Located()
        {
            this.notConnected = true;
            this.blinkTask = Task.Run(async () =>
                {
                    while (this.notConnected)
                    {
                        await this.gameBox.Blink(this.Buttons);
                        await Task.Delay(TimeSpan.FromMilliseconds(500));
                    }
                },
                this.cancellationToken);

            await this.TryConnect();
        }

        private async Task TryConnect()
        {
            if (File.Exists(this.FileName))
            {
                await this.Connect();
            }
            else
            {
                await this.stateMachine.Fire(HueBridgeEvent.Register);
            }
        }

        private async Task Register()
        {
            await Policy
                .Handle<LinkButtonNotPressedException>()
                .WaitAndRetryAsync(
                    30,
                    i => TimeSpan.FromSeconds(2))
                .ExecuteAsync(async () =>
                {
                    var registered = await LocalHueClient.RegisterAsync(this.bridge.IpAddress, nameof(HueGame),
                        "JuniorGameBox", true);

                    var connectionProperties = new HueConnectionProperties
                    {
                        AppKey = registered.Username,
                        StreamingKey = registered.StreamingClientKey
                    };

                    File.WriteAllText(
                        this.FileName,
                        JsonConvert.SerializeObject(connectionProperties));

                    await this.Connect();
                });
        }

        private async Task Connect()
        {
            var content = File.ReadAllText(this.FileName);
            var connectionProperties = JsonConvert.DeserializeObject<HueConnectionProperties>(content);

            this.client = new LocalHueClient(this.bridge.IpAddress, connectionProperties.AppKey);
            this.client.InitializeStreaming(connectionProperties.StreamingKey);

            if (await this.client.CheckConnection())
            {
                await this.stateMachine.Fire(HueBridgeEvent.Connected);
            }
        }
    }

    public class HueLightButton : IDisposable
    {
        private readonly IGameBox gameBox;
        private readonly IHueClient hueClient;
        private readonly ILightableButton lightableButton;
        private Light light;
        private IDisposable subscription;

        public HueLightButton(IGameBox gameBox, IHueClient hueClient, Light light, ILightableButton lightableButton)
        {
            this.gameBox = gameBox;
            this.hueClient = hueClient;
            this.light = light;
            this.lightableButton = lightableButton;
        }

        public void Dispose()
        {
            this.lightableButton.SetLight(false);
            this.subscription?.Dispose();
        }

        public async Task Start()
        {
            if (this.light.State.On)
            {
                await this.lightableButton.SetLight(true);
            }

            this.subscription = this.lightableButton.ButtonUp.Subscribe(async identifier => await this.ToggleLight());
        }

        private async Task ToggleLight()
        {
            this.light = await this.hueClient.GetLightAsync(this.light.Id);
            if (this.light.State.On)
            {
                var lightCommand = new LightCommand().TurnOff();

                await this.hueClient.SendCommandAsync(lightCommand, new[] {this.light.Id});
                await this.lightableButton.SetLight(false);
            }
            else
            {
                var lightCommand = new LightCommand().TurnOn();

                await this.hueClient.SendCommandAsync(lightCommand, new[] {this.light.Id});
                await this.lightableButton.SetLight(true);
            }
        }
    }

    public class HueConnectionProperties
    {
        public HueConnectionProperties()
        {
            this.AppKey = "8PKf0fiwy3y0p0i1XKm7nkhdhGq734Y3X0cVi2BZ";
            this.StreamingKey = "624B5BC3E8E3B189EF3E708FB3B0EA93";
        }

        public string StreamingKey { get; set; }

        public string AppKey { get; set; }
    }
}