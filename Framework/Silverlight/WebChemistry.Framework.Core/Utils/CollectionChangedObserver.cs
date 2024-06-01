namespace WebChemistry.Framework.Utils
{
    using System.Collections.Specialized;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Reactive.Concurrency;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// A weak collection changed observer.
    /// A reference to this object has to be kept in order for the observers to remain active.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    public class CollectionChangedObserver<TElement> : IDisposable
    {
        bool disposed = false;
        Action onReset;
        Action<TElement> onAdd, onRemove;
        IScheduler scheduler;

        void CollectionChanged(INotifyCollectionChanged collection, NotifyCollectionChangedEventArgs args)
        {
            if (disposed) return;

            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (onAdd == null) return;
                    foreach (TElement item in args.NewItems)
                    {
                        var it = item;
                        if (scheduler != null) scheduler.Schedule(() => onAdd(it));
                        else onAdd(it);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    //throw new NotSupportedException("Replace action is not supported.");
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (onRemove == null) return;
                    foreach (TElement item in args.OldItems)
                    {
                        var it = item;
                        if (scheduler != null) scheduler.Schedule(() => onRemove(it));
                        else onRemove(it);
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    if (onReset != null)
                    {
                        if (scheduler != null) scheduler.Schedule(() => onReset());
                        else onReset();
                    }
                    break;
            }
        }

        /// <summary>
        /// Assigns a method to be called when an element is added.
        /// </summary>
        /// <param name="onAdd"></param>
        /// <returns></returns>
        public CollectionChangedObserver<TElement> OnAdd(Action<TElement> onAdd)
        {
            if (this.onAdd != null) throw new InvalidOperationException("onAdd handler already assigned.");

            this.onAdd = onAdd;
            return this;
        }

        /// <summary>
        /// Assigns a method to be called when an element is removed.
        /// </summary>
        /// <param name="onRemove"></param>
        /// <returns></returns>
        public CollectionChangedObserver<TElement> OnRemove(Action<TElement> onRemove)
        {
            if (this.onRemove != null) throw new InvalidOperationException("onRemove handler already assigned.");

            this.onRemove = onRemove;
            return this;
        }


        /// <summary>
        /// Assigns a method to be called when the collection is reset.
        /// </summary>
        /// <param name="onReset"></param>
        /// <returns></returns>
        public CollectionChangedObserver<TElement> OnReset(Action onReset)
        {
            if (this.onReset != null) throw new InvalidOperationException("onReset handler already assigned.");

            this.onReset = onReset;
            return this;
        }

        /// <summary>
        /// Creates a weak collection changed observer.
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="scheduler"></param>
        public CollectionChangedObserver(INotifyCollectionChanged collection, IScheduler scheduler = null)
        {
            this.scheduler = scheduler;
            var listener = 
                new WeakEventListener<
                    CollectionChangedObserver<TElement>, 
                    INotifyCollectionChanged, 
                    NotifyCollectionChangedEventArgs>(this, collection);
            collection.CollectionChanged += listener.OnEvent;
            listener.OnDetachAction = (l, s) => s.CollectionChanged -= l.OnEvent;
            listener.OnEventAction = (l, s, a) => l.CollectionChanged(s, a);
        }

        /// <summary>
        /// By disposing the object, the collection changed events are ignored.
        /// additionally, it is recommended to set the variable to null.
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Dispose()
        {
            disposed = true;
            onReset = null;
            onRemove = null;
            onAdd = null;
            scheduler = null;
        }
    }
}
