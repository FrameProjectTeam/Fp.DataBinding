using System;

using Fp.ObjectModels.Disposing;

using UniRx;

namespace Fp.DataBinding.Binds
{
    public sealed class CommandBind<T> : DisposableObject
    {
        private readonly ReactiveCommand<T> _context;

        private readonly CompositeDisposable _relativeResources = new CompositeDisposable();

        private int _locked;

        private readonly IObserver<T> _observer;
        private readonly RelationType _relationType;

        public CommandBind(ReactiveCommand<T> context, IObservable<T> observable, IObserver<T> observer, RelationType relationType)
        {
            _context = context;
            _relationType = relationType;

            _observer = observer;

            if (_observer != null)
            {
                _context.Subscribe(OnContextChange).AddTo(_relativeResources);
            }

            observable?.Subscribe(OnTargetChange).AddTo(_relativeResources);
        }

        protected override void OnDispose()
        {
            _relativeResources.Dispose();
        }

        private void OnContextChange(T value)
        {
            if (_relationType == RelationType.OneWay)
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
                throw new InvalidOperationException("Signal was execute recycled");
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
                    _context.Execute(value);
                }
                finally
                {
                    _locked--;
                }
            }
            else if (_locked > 0)
            {
                throw new InvalidOperationException("Signal was execute recycled");
            }
        }
    }
}