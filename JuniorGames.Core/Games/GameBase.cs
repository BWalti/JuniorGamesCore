namespace JuniorGames.Core.Games
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using JuniorGames.Core.Framework;

    /// <summary>
    ///     Abstract base class for games, implementing and providing the <see cref="CancellationToken" />.
    /// </summary>
    public abstract class GameBase : IGame
    {
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;

        protected GameBase(IGameBox box)
        {
            this.GameBox = box;
        }

        protected IGameBox GameBox { get; }

        protected CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        public void Dispose()
        {
            this.Dispose(true);
        }

        public Task Start(TimeSpan maximumGameTime)
        {
            this.cancellationTokenSource = new CancellationTokenSource(maximumGameTime);
            return this.OnStart();
        }

        public virtual void Stop()
        {
            this.cancellationTokenSource.Cancel();
        }

        protected abstract Task OnStart();

        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.disposed = true;
            }
        }
    }
}