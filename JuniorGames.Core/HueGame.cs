namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using GameBox.Framework;
    using Q42.HueApi;
    using Q42.HueApi.Interfaces;
    using Serilog;

    public class HueGame : GameBase
    {
        private readonly HueGameOptions options;

        public HueGame(IBox box, HueGameOptions options) : base(box)
        {
            this.options = options;
        }

        public List<ILightableButton> PlayerOneButtons { get; private set; }
        public List<ILightableButton> PlayerTwoButtons { get; private set; }

        protected override async Task OnStart()
        {
            // locate bridges
            // find matching auth keys & connect
            // if no auth key detected, try to register -> save auth key & connect
            // connected: upon selection of bridge (Player 1)
            // list available (& connected) lights in Player 2 field
            // upon light button: toggle light from on-off and off-random_on (hue / saturation / intensity)

            Log.Information("Starting HueGame...");

            try
            {
                IBridgeLocator locator = new HttpBridgeLocator();
                var bridgeIPs = (await locator.LocateBridgesAsync(TimeSpan.FromSeconds(30))).ToList();

                Log.Information($"Found {bridgeIPs.Count} bridges");

                this.PlayerOneButtons = this.Box.LedButtonPinPins.Where(b => b.Player == Player.One).ToList();
                this.PlayerTwoButtons = this.Box.LedButtonPinPins.Where(b => b.Player == Player.Two).ToList();

                var hueStateMachines = bridgeIPs.Zip(this.PlayerOneButtons,
                        (bridge, button) => new HueStateMachine(this.Box, bridge, button, this.PlayerTwoButtons,
                            this.CancellationToken))
                    .ToList();
                foreach (var hueStateMachine in hueStateMachines)
                {
                    hueStateMachine.OnSelected += () => hueStateMachines
                        .Except(new[] {hueStateMachine})
                        .ToList()
                        .ForEach(async hsm => await hsm.DoUnselect());
                }

                Log.Information("Created HueStateMachines");

                await Task.WhenAll(hueStateMachines.Select(hsm => hsm.Start()).ToArray());
                await Task.Delay(TimeSpan.FromMinutes(3), this.CancellationToken);
            }
            catch (Exception e)
            {
                Log.Error(e, "Exception while playing HueGame...");
            }
        }
    }

    public class HueGameOptions : IOptions
    {
    }
}