namespace JuniorGames.Core.Games
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Q42.HueApi;
    using Q42.HueApi.Interfaces;
    using Serilog;

    public class HueGame : GameBase
    {
        private readonly HueGameOptions options;
        private IDisposable connectionSubscription;

        public HueGame(IGameBox box, HueGameOptions options) : base(box)
        {
            this.options = options;
        }

        protected override async Task Start()
        {
            IBridgeLocator locator = new HttpBridgeLocator();
            var bridgeIPs = (await locator.LocateBridgesAsync(TimeSpan.FromSeconds(5))).ToList();
            var clients = bridgeIPs.Select(bridge => new LocalHueClient(bridge.IpAddress)).ToList();

            foreach (var bridge in bridgeIPs)
            {
                Log.Information($"Located Bridge at IP: {bridge.IpAddress}");
            }

            var client = bridgeIPs.FirstOrDefault(ip => ip.IpAddress == this.options.IpAddress);
            if (client != null)
            {
                var localHueClient = new LocalHueClient(client.IpAddress, this.options.Username);

                if (await localHueClient.CheckConnection())
                {
                    var lights = await localHueClient.GetLightsAsync();
                    foreach (var light in lights)
                    {
                        Log.Information($"{light.Name}: {light.State.On}");
                    }
                }
            }
            else
            {
                this.RegisterHueConnection(clients);
            }

            //Log.Information("Connecting to Hue...");
            //var client = new LocalHueClient(this.options.IpAddress, this.options.Key);

            //if (await client.CheckConnection())
            //{
            //    Log.Information($"Connected!");
            //    var lights = await client.GetLightsAsync();

            //    foreach (var light in lights)
            //    {
            //        Log.Verbose($"Found light: {light.Name} ({light.ManufacturerName})");
            //    }
            //}
        }

        private void RegisterHueConnection(List<LocalHueClient> clients)
        {
            var eachSecond = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);

            this.connectionSubscription = eachSecond.Subscribe(async _ =>
            {
                var tasks = clients
                    .Select(client => client.RegisterAsync("HueGame", "JuniorGameBox", true))
                    .ToList();

                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (Exception e)
                {
                    Log.Warning(e.Message);

                    var successfulTask = tasks.FirstOrDefault(t => t.IsCompleted);
                    if (successfulTask != null)
                    {
                        Log.Information(
                            $"Successfully connected with: {successfulTask.Result.Ip}, with {successfulTask.Result.Username} / {successfulTask.Result.StreamingClientKey}");
                    }
                }
            });
        }
    }

    public class HueGameOptions : IOptions
    {
        public HueGameOptions()
        {
            this.IpAddress = "192.168.0.105";
            this.Username = "8PKf0fiwy3y0p0i1XKm7nkhdhGq734Y3X0cVi2BZ";
            this.Key = "624B5BC3E8E3B189EF3E708FB3B0EA93";
        }

        public string Username { get; set; }

        public string Key { get; set; }

        public string IpAddress { get; set; }
    }
}