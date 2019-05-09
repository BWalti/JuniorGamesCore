namespace JuniorGamesCore
{
    using System;
    using System.Device.Gpio;
    using System.Threading.Tasks;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using JuniorGames.Core;
    using JuniorGames.Core.Framework;
    using JuniorGames.Hardware;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .MinimumLevel.Verbose()
                .CreateLogger();

            Log.Information("Serilog configured...");

            using (var bootstrapper = new GameBootstrapper(HardwareRegistrations))
            {
                Log.Information("Bootstrapper created...");

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
            Log.Information("Creating GpioController...");
            var gpioController = new GpioController(PinNumberingScheme.Logical);

            container.Register(Component.For<GpioController>().Instance(gpioController).LifestyleSingleton());
            container.Register(Component.For<IGameBox>().ImplementedBy<GameBox>().LifestyleSingleton());
        }
    }
}