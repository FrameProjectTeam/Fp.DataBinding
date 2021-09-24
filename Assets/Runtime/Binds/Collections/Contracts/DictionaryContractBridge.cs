using System.Linq;

using Fp.ObjectModels.Disposing;

using UniRx;

using UnityEngine;
using UnityEngine.Assertions;

namespace Fp.DataBinding.Binds.Collections.Contracts
{
    public sealed class DictionaryContractBridge<TKey, TSource, TTarget> : DisposableObject
    {
        private readonly uint _targetKey;

        private readonly IConvertContract<TSource, TTarget> _contract;

        private readonly ReactiveDictionary<TKey, TTarget> _dictionaryB;

        private readonly ICollectionBind _bindB;

        private readonly CompositeDisposable _dependentResource = new CompositeDisposable();

        public DictionaryContractBridge(
            uint sourceKey,
            uint targetKey,
            BindContext bindContext,
            IConvertContract<TSource, TTarget> contract)
        {
            Assert.IsNotNull(bindContext);
            Assert.IsNotNull(contract);

            _targetKey = targetKey;
            _contract = contract;

            var dictionaryA = new ReactiveDictionary<TKey, TSource>();
            _dictionaryB = new ReactiveDictionary<TKey, TTarget>();

            _dependentResource.Add(dictionaryA);
            _dependentResource.Add(_dictionaryB);

            ICollectionBind bindA = bindContext.CreateDictionaryBind(sourceKey, dictionaryA);
            _bindB = bindContext.CreateDictionaryBind(_targetKey, _dictionaryB);

            bindA.AddTo(_dependentResource);
            _bindB.AddTo(_dependentResource);

            dictionaryA.ObserveAdd().Subscribe(OnAdd).AddTo(_dependentResource);
            dictionaryA.ObserveReplace().Subscribe(OnReplace).AddTo(_dependentResource);
            dictionaryA.ObserveReset().Subscribe(OnReset).AddTo(_dependentResource);
            dictionaryA.ObserveRemove().Subscribe(OnRemove).AddTo(_dependentResource);

            _dictionaryB.ObserveAdd().Subscribe(d => BrokeBridgeWasChanged()).AddTo(_dependentResource);
            _dictionaryB.ObserveReplace().Subscribe(d => BrokeBridgeWasChanged()).AddTo(_dependentResource);
            _dictionaryB.ObserveReset().Subscribe(d => BrokeBridgeWasChanged()).AddTo(_dependentResource);
            _dictionaryB.ObserveRemove().Subscribe(d => BrokeBridgeWasChanged()).AddTo(_dependentResource);
        }

        protected override void OnDispose()
        {
            _dependentResource.Dispose();
        }

        private void BrokeBridgeWasChanged()
        {
            if (_bindB.IsLastInitiator && _bindB.IsLastInteractor && _bindB.ActiveDepth == 0)
            {
                return;
            }

            Debug.LogError(
                $"[UiBindContext] Dictionary contract bridge will be broke: Dependent dictionary {_targetKey}:{typeof(TTarget).Name} has been changed");
            Dispose();
        }

        private void OnAdd(DictionaryAddEvent<TKey, TSource> e)
        {
            _dictionaryB.Add(e.Key, _contract.Create(e.Value));
        }

        private void OnReplace(DictionaryReplaceEvent<TKey, TSource> e)
        {
            _contract.Destroy(_dictionaryB[e.Key]);

            _dictionaryB[e.Key] = _contract.Create(e.NewValue);
        }

        private void OnReset(Unit e)
        {
            TTarget[] buffer = _dictionaryB.Values.ToArray();

            _dictionaryB.Clear();

            foreach (TTarget t in buffer)
            {
                _contract.Destroy(t);
            }
        }

        private void OnRemove(DictionaryRemoveEvent<TKey, TSource> e)
        {
            TTarget removed = _dictionaryB[e.Key];
            _dictionaryB.Remove(e.Key);
            _contract.Destroy(removed);
        }
    }
}