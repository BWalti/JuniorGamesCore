namespace SimulatorBox
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Media;

    using Caliburn.Micro;

    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using GameBox.Framework;
    using JuniorGames.GamesClean;

    public class ShellViewModel : Screen
    {
        private SimpleGame game;

        //private readonly GameBootstrapper bootstrapper;

        public ShellViewModel()
        {
            //this.bootstrapper = new GameBootstrapper(SimulatorRegistrations);
            //var gameBox = this.bootstrapper.Box;

            var boxBaseOptions = new BoxBaseOptions
            {
                IdleTimeout = TimeSpan.FromMinutes(3)
            };
            var gameBox = new BoxSimulator(boxBaseOptions);

            this.game = new SimpleGame(gameBox, new SimpleGameOptions
            {
                LightUp = TimeSpan.FromMilliseconds(400),
                Pause = TimeSpan.FromMilliseconds(200),
                Retries = 3
            });

            this.GreenOne = new LightyButtonViewModel(Colors.Green, GetLightableButton(gameBox, BoxBase.GreenOneButtonIdentifier));
            this.YellowOne = new LightyButtonViewModel(Colors.Gold, GetLightableButton(gameBox, BoxBase.YellowOneButtonIdentifier));
            this.RedOne = new LightyButtonViewModel(Colors.Red, GetLightableButton(gameBox, BoxBase.RedOneButtonIdentifier));
            this.BlueOne = new LightyButtonViewModel(Colors.Blue, GetLightableButton(gameBox, BoxBase.BlueOneButtonIdentifier));
            this.WhiteOne = new LightyButtonViewModel(Colors.LightGray, GetLightableButton(gameBox, BoxBase.WhiteOneButtonIdentifier));

            this.GreenTwo = new LightyButtonViewModel(Colors.Green, GetLightableButton(gameBox, BoxBase.GreenTwoButtonIdentifier));
            this.YellowTwo = new LightyButtonViewModel(Colors.Gold, GetLightableButton(gameBox, BoxBase.YellowTwoButtonIdentifier));
            this.RedTwo = new LightyButtonViewModel(Colors.Red, GetLightableButton(gameBox, BoxBase.RedTwoButtonIdentifier));
            this.BlueTwo = new LightyButtonViewModel(Colors.Blue, GetLightableButton(gameBox, BoxBase.BlueTwoButtonIdentifier));
            this.WhiteTwo = new LightyButtonViewModel(Colors.LightGray, GetLightableButton(gameBox, BoxBase.WhiteTwoButtonIdentifier));
        }

        private static LightyButtonModel GetLightableButton(IBox box, ButtonIdentifier buttonIdentifier)
        {
            return box[buttonIdentifier] as LightyButtonModel;
        }

        public LightyButtonViewModel GreenOne { get; }
        public LightyButtonViewModel YellowOne { get; }
        public LightyButtonViewModel RedOne { get; }
        public LightyButtonViewModel BlueOne { get; }
        public LightyButtonViewModel WhiteOne { get; }

        public LightyButtonViewModel GreenTwo { get; }
        public LightyButtonViewModel YellowTwo { get; }
        public LightyButtonViewModel RedTwo { get; }
        public LightyButtonViewModel BlueTwo { get; }
        public LightyButtonViewModel WhiteTwo { get; }


        public async Task Start()
        {
            await this.game.Start();

            //using (var chooser = this.bootstrapper.GameChooser())
            //{
            //    await chooser.Start(TimeSpan.FromMinutes(5));
            //}
        }

        public void Reset()
        {
            //this.Simulator.DoReset();
        }

        //public BoxSimulator Simulator => this.bootstrapper.Box as BoxSimulator;

        private static void SimulatorRegistrations(IWindsorContainer container)
        {
            container.Register(Component.For<IBox>().ImplementedBy<BoxSimulator>().LifestyleSingleton());
        }
    }
}