namespace JukeBox
{
    using System;
    using System.Device.Gpio;
    using System.Threading;
    using System.Threading.Tasks;
    using ByteDev.Sonos;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using GameBox.Framework;
    using Serilog;
    using Serilog.Sinks.SystemConsole.Themes;
    using Stateless;

    internal class Program
    {
        private static SonosController controller;
        private static IBox box;

        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .MinimumLevel.Information()
                .CreateLogger();

            Log.Verbose("Serilog configured...");

            //var container = new WindsorContainer();
            //HardwareRegistrations(container);
            //container.Register(Component.For<BoxBaseOptions>()
            //    .Instance(new BoxBaseOptions
            //    {
            //        IdleTimeout = TimeSpan.FromSeconds(30)
            //    }));

            //box = container.Resolve<IBox>();

            //await box.BlinkAll(2);

            var davin = "192.168.10.219";
            var buro = "192.168.10.90";

            controller = new SonosControllerFactory().Create(buro);

            var browseResponse = await controller.GetQueueAsync();

            var isPlayingAsync = await controller.GetIsPlayingAsync();
            await controller.PlayAsync();
        }
        
        //private async Task PlayPauseButton(CancellationToken token)
        //{
        //    while (!token.IsCancellationRequested)
        //    {
        //        var isPlayingAsync = await controller.GetIsPlayingAsync();
        //        await box.Blink(new[] {BoxBase.RedTwoButtonIdentifier}, 5, 500);
        //    }
        //}

        private static void HardwareRegistrations(IWindsorContainer container)
        {
            Log.Verbose("Creating GpioController...");
            var gpioController = new GpioController(PinNumberingScheme.Logical);

            container.Register(Component.For<GpioController>().Instance(gpioController).LifestyleSingleton());
            container.Register(Component.For<IBox>().ImplementedBy<Box>().LifestyleSingleton());
        }
    }
}