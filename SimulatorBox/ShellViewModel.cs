namespace SimulatorBox
{
    using System;
    using System.Windows.Media;

    using Caliburn.Micro;

    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using JuniorGames.Core;
    using JuniorGames.Core.Framework;

    public class ShellViewModel : Screen
    {
        private readonly GameBootstrapper bootstrapper;

        public ShellViewModel()
        {
            this.bootstrapper = new GameBootstrapper(SimulatorRegistrations);
            var gameBox = this.bootstrapper.GameBox;

            this.GreenOne = new LightyButtonViewModel(Colors.Green, GetLightableButton(gameBox, GameBoxBase.GreenOneButtonIdentifier));
            this.YellowOne = new LightyButtonViewModel(Colors.Yellow, GetLightableButton(gameBox, GameBoxBase.YellowOneButtonIdentifier));
            this.RedOne = new LightyButtonViewModel(Colors.Red, GetLightableButton(gameBox, GameBoxBase.RedOneButtonIdentifier));
            this.BlueOne = new LightyButtonViewModel(Colors.Blue, GetLightableButton(gameBox, GameBoxBase.BlueOneButtonIdentifier));
            this.WhiteOne = new LightyButtonViewModel(Colors.LightGray, GetLightableButton(gameBox, GameBoxBase.WhiteOneButtonIdentifier));

            this.GreenTwo = new LightyButtonViewModel(Colors.Green, GetLightableButton(gameBox, GameBoxBase.GreenTwoButtonIdentifier));
            this.YellowTwo = new LightyButtonViewModel(Colors.Yellow, GetLightableButton(gameBox, GameBoxBase.YellowTwoButtonIdentifier));
            this.RedTwo = new LightyButtonViewModel(Colors.Red, GetLightableButton(gameBox, GameBoxBase.RedTwoButtonIdentifier));
            this.BlueTwo = new LightyButtonViewModel(Colors.Blue, GetLightableButton(gameBox, GameBoxBase.BlueTwoButtonIdentifier));
            this.WhiteTwo = new LightyButtonViewModel(Colors.LightGray, GetLightableButton(gameBox, GameBoxBase.WhiteTwoButtonIdentifier));
        }

        private static LightyButtonModel GetLightableButton(IGameBox gameBox, ButtonIdentifier buttonIdentifier)
        {
            return gameBox[buttonIdentifier] as LightyButtonModel;
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


        public async void Start()
        {
            using (var chooser = this.bootstrapper.GameChooser())
            {
                await chooser.Start(TimeSpan.FromMinutes(5));
            }
        }

        private static void SimulatorRegistrations(IWindsorContainer container)
        {
            container.Register(Component.For<IGameBox>().ImplementedBy<GameBoxSimulator>().LifestyleSingleton());
        }
    }
}