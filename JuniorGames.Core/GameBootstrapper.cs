namespace JuniorGames.Core
{
    using System;
    using Castle.MicroKernel.Registration;
    using Castle.Windsor;
    using JuniorGames.Core.Framework;
    using JuniorGames.Core.Games;

    public class GameBootstrapper : IDisposable
    {
        private WindsorContainer container;

        public GameBootstrapper(Action<IWindsorContainer> containerConfigureAction = null)
        {
            this.Configure(containerConfigureAction);

            var gameBoxOptions = this.container.Resolve<GameBoxOptions>();
            gameBoxOptions.IdleTimeout = TimeSpan.FromSeconds(10);
        }

        public IGameBox GameBox => this.container.Resolve<IGameBox>();
        
        public IGame ChainGame(int games, int retries = 3)
        {
            var options = this.container.Resolve<ChainGameOptions>();
            options.Games = games;
            options.Retries = retries;

            return this.container.Resolve<ChainGame>();
        }

        //public IGame HueGame()
        //{
        //    return this.container.Resolve<HueGame>();
        //}

        public IGame LedDemo(int lightMs = 200, int darkMs = 100)
        {
            var options = this.container.Resolve<LedDemoOptions>();
            options.LightMs = lightMs;
            options.DarkMs = darkMs;

            return this.container.Resolve<LedDemoGame>();
        }

        public IGame AfterButner()
        {
            return this.container.Resolve<AfterBurnerGame>();
        }

        public IGame LightifyOnButtonPress()
        {
            return this.container.Resolve<LightifyOnPressGame>();
        }

        public IGame GameChooser()
        {
            var options = this.container.Resolve<GameChooserOptions>();
            options.MaximumGameTimeMinutes = 10;

            return this.container.Resolve<GameChooserGame>();
        }

        private void Configure(Action<IWindsorContainer> containerConfigureAction)
        {
            this.container = new WindsorContainer();

            containerConfigureAction(this.container);

            this.container.Register(Component.For<GameBootstrapper>().Instance(this).LifestyleSingleton());

            //this.container.Register(
            //    Component
            //        .For<IEventAggregator>()
            //        .ImplementedBy<EventAggregator>()
            //        .LifestyleSingleton());

            this.container.Register(
                Classes
                    .FromAssemblyContaining(typeof(GameBootstrapper))
                    .BasedOn<IGame>()
                    .WithServiceSelf()
                    .LifestyleTransient());

            this.container.Register(
                Classes
                    .FromAssemblyContaining(typeof(GameBootstrapper))
                    .BasedOn<IOptions>()
                    .WithServiceSelf()
                    .LifestyleSingleton());
        }

        public IGame FastBlinky()
        {
            return this.container.Resolve<FastBlinkyGame>();
        }
        
        public IGame TwoPlayers()
        {
            return this.container.Resolve<TwoPlayersGame>();
        }

        public void Dispose()
        {
            this.container?.Dispose();
        }
    }
}