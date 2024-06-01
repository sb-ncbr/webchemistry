namespace WebChemistry.Framework.Utils
{
    using System.ComponentModel;
    using System.Collections.Generic;
    using WebChemistry.Framework.Core;
    using System.Reactive.Subjects;

    public class PropertyChangedObserver : ISubject<string>
    {
        Subject<string> inner = new Subject<string>();

        void EventAction(string propertyName)
        {
            OnNext(propertyName);
        }

        public PropertyChangedObserver(INotifyPropertyChanged obj)
        {
            obj.ObservePropertyChanged(this, (l, s, p) => l.EventAction(p));
        }

        public void OnCompleted()
        {
            inner.OnCompleted();
        }

        public void OnError(System.Exception exception)
        {
            inner.OnError(exception);
        }

        public void OnNext(string value)
        {
            inner.OnNext(value);
        }

        public System.IDisposable Subscribe(System.IObserver<string> observer)
        {
            return inner.Subscribe(observer);
        }
    }
}
