using BoxBaseOptions = GameBox.Framework.BoxBaseOptions;
using IBox = GameBox.Framework.IBox;

namespace JuniorGames
{
    using System;
    using System.Device.Gpio;
    using System.Threading.Tasks;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using JuniorGames.GamesClean;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .MinimumLevel.Information()
                .CreateLogger();

            Log.Verbose("Serilog configured...");

            var container = new WindsorContainer();
            HardwareRegistrations(container);
            container.Register(Component.For<BoxBaseOptions>()
                .Instance(new BoxBaseOptions
                {
                    IdleTimeout = TimeSpan.FromMinutes(5)
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