namespace GameBox.Framework
{
    using System;
    using System.Collections.Generic;

    public class CollectionDisposable : IDisposable
    {
        private readonly IEnumerable<IDisposable> disposables;

        public CollectionDisposable(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        public virtual void Dispose()
        {
            foreach (var disposable in this.disposables)
            {
                disposable.Dispose();
            }
        }
    }
}