using System;

using Fp.ObjectModels.Disposing;

using UniRx;

namespace Fp.DataBinding.Binds
{
    public sealed class ValueBind<T> : DisposableObject
    {
        private readonly ReactiveValue<T> _context;

        private readonly CompositeDisposable _relativeResources = new CompositeDisposable();

        private int _locked;

        private readonly IObserver<T> _observer;
        private readonly RelationType _relationType;

        public ValueBind(ReactiveValue<T> context, IObservable<T> observable, IObserver<T> observer, RelationType relationType)
        {
            _context = context;
            _relationType = relationType;

            _observer = observer;
            _context.Subscribe(OnContextChange, OnError, OnCompleted).AddTo(_relativeResources);

            observable?.Subscribe(OnTargetChange).AddTo(_relativeResources);
        }

        protected override void OnDispose()
        {
            _relativeResources.Dispose();
        }

        private void OnCompleted()
        {
            _observer?.OnCompleted();
            Dispose();
        }

        private void OnError(Exception error)
        {
            _observer?.OnError(error);
            Dispose();
        }

        private void OnContextChange(T value)
        {
            if (_relationType == RelationType.OneWay || _observer == null)
            {
                return;
            }

            if (_locked % 2 == 0)
            {
                try
                {
                    _locked--;
                    _observer.OnNext(value);
                }
                finally
                {
                    _locked++;
                }
            }
            else if (_locked < 0)
            {
                throw new InvalidOperationException("Properties was modified from observer call");
            }
        }

        private void OnTargetChange(T value)
        {
            if (_relationType == RelationType.FromSource)
            {
                return;
            }

            if (_locked % 2 == 0)
            {
                try
                {
                    _locked++;
                    _context.OnNext(value);
                }
                finally
                {
                    _locked--;
                }
            }
            else if (_locked > 0)
            {
                throw new InvalidOperationException("Properties was modified from observer call");
            }
        }
    }
}