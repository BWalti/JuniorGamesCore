namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Q42.HueApi;
    using Q42.HueApi.Interfaces;

    public class HueGame : GameBase
    {
        private readonly HueGameOptions options;

        public HueGame(IGameBox box, HueGameOptions options) : base(box)
        {
            this.options = options;
        }

        public List<ILightableButton> PlayerOneButtons { get; private set; }
        public List<ILightableButton> PlayerTwoButtons { get; private set; }

        protected override async Task Start()
        {
            // locate bridges
            // find matching auth keys & connect
            // if no auth key detected, try to register -> save auth key & connect
            // connected: upon selection of bridge (Player 1)
            // list available (& connected) lights in Player 2 field
            // upon light button: toggle light from on-off and off-random_on (hue / saturation / intensity)

            IBridgeLocator locator = new HttpBridgeLocator();
            var bridgeIPs = (await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5))).ToList();

            this.PlayerOneButtons = this.GameBox.LedButtonPinPins.Where(b => b.Player == Player.One).ToList();
            this.PlayerTwoButtons = this.GameBox.LedButtonPinPins.Where(b => b.Player == Player.Two).ToList();

            var hueStateMachines = bridgeIPs.Zip(this.PlayerOneButtons,
                (bridge, button) => new HueStateMachine(this.GameBox, bridge, button, this.PlayerTwoButtons,
                    this.CancellationToken)).ToList();
            foreach (var hueStateMachine in hueStateMachines)
            {
                hueStateMachine.OnSelected += () => hueStateMachines
                    .Except(new[] {hueStateMachine})
                    .ToList()
                    .ForEach(async hsm => await hsm.DoUnselect());
            }

            await Task.WhenAll(hueStateMachines.Select(hsm => hsm.Start()).ToArray());
            await Task.Delay(TimeSpan.FromMinutes(3), this.CancellationToken);
        }
    }

    public class HueGameOptions : IOptions
    {
    }
}