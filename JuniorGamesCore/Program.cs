namespace JuniorGamesCore
{
    using System;
    using System.Device.Gpio;
    using System.Threading.Tasks;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using JuniorGames.Core;
    using JuniorGames.Core.Framework;
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

            using (var bootstrapper = new GameBootstrapper(HardwareRegistrations))
            {
                Log.Verbose("Bootstrapper created...");

                var gameBox = bootstrapper.GameBox;
                Log.Information("gameBox created!");

                await gameBox.BlinkAll(3);

                using (var chooser = bootstrapper.GameChooser())
                {
                    await chooser.Start(TimeSpan.FromMinutes(5));
                }
            }
        }

        private static void HardwareRegistrations(IWindsorContainer container)
        {
            Log.Verbose("Creating GpioController...");
            var gpioController = new GpioController(PinNumberingScheme.Logical);

            container.Register(Component.For<GpioController>().Instance(gpioController).LifestyleSingleton());
            container.Register(Component.For<IGameBox>().ImplementedBy<GameBox>().LifestyleSingleton());
        }
    }
}