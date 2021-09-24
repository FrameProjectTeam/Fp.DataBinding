using System;

using UniRx;

namespace Fp.DataBinding.Binds
{
    public sealed class ReactiveValue<T> : IDisposable, IReadOnlyReactiveProperty<T>, IObserver<T>
    {
        private readonly ReadOnlyReactiveProperty<T> _property;
        private readonly Subject<T> _subject;

        public ReactiveValue()
        {
            _subject = new Subject<T>();
            _property = new ReadOnlyReactiveProperty<T>(_subject, false);
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            _property.Dispose();

            OnCompleted();
            _subject.Dispose();
        }

        #endregion

        #region IObservable<T> Implementation

        public IDisposable Subscribe(IObserver<T> observer)
        {
            return _property.Subscribe(observer);
        }

        #endregion

        #region IObserver<T> Implementation

        public void OnCompleted()
        {
            _subject.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _subject.OnError(error);
        }

        public void OnNext(T value)
        {
            _subject.OnNext(value);
        }

        #endregion

        #region IReadOnlyReactiveProperty<T> Implementation

        public T Value => _property.Value;

        public bool HasValue => _property.HasValue;

        #endregion
    }
}