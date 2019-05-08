namespace JuniorGames.Core.Games
{
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;
    using Q42.HueApi;

    public class HueGame : GameBase
    {
        private readonly HueGameOptions options;

        public HueGame(IGameBox box, HueGameOptions options) : base(box)
        {
            this.options = options;
        }

        protected override async Task Start()
        {
            var client = new LocalHueClient(this.options.IpAddress, this.options.Key);
            if (await client.CheckConnection())
            {
                var lights = await client.GetLightsAsync();
            }
        }
    }

    public class HueGameOptions : IOptions
    {
        public HueGameOptions()
        {
            this.IpAddress = "192.168.0.105";
            this.Key = "G2dJQRwQn2Qw2Eu7C7GvXufm5nrVQ5radG0WKB1B";
        }

        public string Key { get; set; }

        public string IpAddress { get; set; }
    }
}