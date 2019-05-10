namespace JuniorGames.Core.Framework
{
    using System;

    public class CollectionDisposableWithFinalAction : CollectionDisposable
    {
        private readonly Action finalAction;

        public CollectionDisposableWithFinalAction(Action finalAction, params IDisposable[] disposables) : base(
            disposables)
        {
            this.finalAction = finalAction;
        }

        public override void Dispose()
        {
            base.Dispose();
            this.finalAction();
        }
    }
}