namespace JuniorGames.Games
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using GameBox.Framework;

    /// <summary>
    ///     Abstract base class for games, implementing and providing the <see cref="CancellationToken" />.
    /// </summary>
    public abstract class GameBase : IGame
    {
        private CancellationTokenSource cancellationTokenSource;
        private bool disposed;
        private TaskCompletionSource<bool> taskCompletionSource;

        protected GameBase(IBox box)
        {
            this.Box = box;
        }

        protected IBox Box { get; }

        protected CancellationToken CancellationToken => this.cancellationTokenSource.Token;

        public void Dispose()
        {
            this.Dispose(true);
        }

        public async Task Start(TimeSpan maximumGameTime)
        {
            this.cancellationTokenSource = new CancellationTokenSource(maximumGameTime);

            this.taskCompletionSource = new TaskCompletionSource<bool>();

            await this.OnStart();
            await this.taskCompletionSource.Task;
        }

        protected void NotifyGameComplete(bool success = true)
        {
            this.taskCompletionSource.SetResult(success);
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