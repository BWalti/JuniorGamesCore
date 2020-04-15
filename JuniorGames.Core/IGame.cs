namespace JuniorGames.Games
{
    using System;
    using System.Threading.Tasks;

    public interface IGame : IDisposable
    {
        Task Start(TimeSpan maximumGameTime);

        void Stop();
    }
}