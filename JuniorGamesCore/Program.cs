using BoxBaseOptions = GameBox.Framework.BoxBaseOptions;
using ButtonIdentifier = GameBox.Framework.ButtonIdentifier;
using IBox = GameBox.Framework.IBox;

namespace JuniorGames
{
    using System;
    using System.Device.Gpio;
    using System.Threading.Tasks;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using JuniorGames.GamesClean;
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using Serilog.Core;
    using Serilog.Formatting.Elasticsearch;
    using Serilog.Sinks.Elasticsearch;
    using Serilog.Sinks.SystemConsole.Themes;

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .Destructure.ByTransforming<ButtonIdentifier>(bi => new { Player = bi.Player, Color = bi.Color.Name })
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            Log.Verbose("Serilog configured...");

            var container = new WindsorContainer();
            HardwareRegistrations(container);
            container.Register(Component.For<BoxBaseOptions>()
                .Instance(new BoxBaseOptions
                {
                    IdleTimeout = TimeSpan.FromMinutes(5)
                }));

            container.Register(Component.For<SimpleGameOptions>()
                .Instance(new SimpleGameOptions
                {
                    LightUp = TimeSpan.FromMilliseconds(400),
                    Pause = TimeSpan.FromMilliseconds(200),
                    Retries = 3
                }));

            container.Register(Component.For<SimpleGame>());

            var game = container.Resolve<SimpleGame>();
            await game.Start();
            await game.Result;

            //using (var bootstrapper = new GameBootstrapper(HardwareRegistrations))
            //{
            //    Log.Verbose("Bootstrapper created...");

            //    var gameBox = bootstrapper.Box;
            //    Log.Information("gameBox created!");

            //    await gameBox.BlinkAll(3);

            //    using (var chooser = bootstrapper.GameChooser())
            //    {
            //        await chooser.Start(TimeSpan.FromMinutes(5));
            //    }
            //}
        }

        private static void HardwareRegistrations(IWindsorContainer container)
        {
            Log.Verbose("Creating GpioController...");
            var gpioController = new GpioController(PinNumberingScheme.Logical);

            container.Register(Component.For<GpioController>().Instance(gpioController).LifestyleSingleton());
            container.Register(Component.For<IBox>().ImplementedBy<GameBox>().LifestyleSingleton());
        }
    }
}