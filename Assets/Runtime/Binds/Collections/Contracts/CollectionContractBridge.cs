using System.Linq;

using Fp.ObjectModels.Disposing;

using UniRx;

using UnityEngine;
using UnityEngine.Assertions;

namespace Fp.DataBinding.Binds.Collections.Contracts
{
    public sealed class CollectionContractBridge<TSource, TTarget> : DisposableObject
    {
        private readonly uint _targetKey;

        private readonly IConvertContract<TSource, TTarget> _contract;

        private readonly ReactiveCollection<TTarget> _collectionB;

        private readonly ICollectionBind _bindB;

        private readonly CompositeDisposable _dependentResource = new CompositeDisposable();

        public CollectionContractBridge(
            uint sourceKey,
            uint targetKey,
            BindContext bindContext,
            IConvertContract<TSource, TTarget> contract)
        {
            Assert.IsNotNull(bindContext);
            Assert.IsNotNull(contract);

            _targetKey = targetKey;
            _contract = contract;

            var collectionA = new ReactiveCollection<TSource>();
            _collectionB = new ReactiveCollection<TTarget>();

            _dependentResource.Add(collectionA);
            _dependentResource.Add(_collectionB);

            ICollectionBind bindA = bindContext.CreateCollectionBind(sourceKey, collectionA);
            _bindB = bindContext.CreateCollectionBind(_targetKey, _collectionB);

            bindA.AddTo(_dependentResource);
            _bindB.AddTo(_dependentResource);

#if UNITY_EDITOR
            if (_collectionB.Count > 0)
            {
                throw new BindContextException("Contract bridge can't be created on non empty target array");
            }
#else
            Assert.AreEqual(0, _collectionB.Count);
#endif
            //Copy source array to destination
            foreach (TSource source in collectionA)
            {
                _collectionB.Add(_contract.Create(source));
            }

            collectionA.ObserveAdd().Subscribe(OnAdd).AddTo(_dependentResource);
            collectionA.ObserveReplace().Subscribe(OnReplace).AddTo(_dependentResource);
            collectionA.ObserveMove().Subscribe(OnMove).AddTo(_dependentResource);
            collectionA.ObserveReset().Subscribe(OnReset).AddTo(_dependentResource);
            collectionA.ObserveRemove().Subscribe(OnRemove).AddTo(_dependentResource);

            _collectionB.ObserveAdd().Subscribe(d => BrokeBridgeIfChanged()).AddTo(_dependentResource);
            _collectionB.ObserveReplace().Subscribe(d => BrokeBridgeIfChanged()).AddTo(_dependentResource);
            _collectionB.ObserveMove().Subscribe(d => BrokeBridgeIfChanged()).AddTo(_dependentResource);
            _collectionB.ObserveReset().Subscribe(d => BrokeBridgeIfChanged()).AddTo(_dependentResource);
            _collectionB.ObserveRemove().Subscribe(d => BrokeBridgeIfChanged()).AddTo(_dependentResource);
        }

        protected override void OnDispose()
        {
            _dependentResource.Dispose();
        }

        private void BrokeBridgeIfChanged()
        {
            if (_bindB.IsLastInitiator && _bindB.IsLastInteractor && _bindB.ActiveDepth == 0)
            {
                return;
            }

            Debug.LogError($"[UiBindContext] Array contract bridge will be broke: Dependent array {_targetKey}:{typeof(TTarget).Name} was changed");
            Dispose();
        }

        private void OnAdd(CollectionAddEvent<TSource> e)
        {
            _collectionB.Insert(e.Index, _contract.Create(e.Value));
        }

        private void OnReplace(CollectionReplaceEvent<TSource> e)
        {
            _contract.Destroy(_collectionB[e.Index]);
            _collectionB[e.Index] = _contract.Create(e.NewValue);
        }

        private void OnMove(CollectionMoveEvent<TSource> e)
        {
            _collectionB.Move(e.OldIndex, e.NewIndex);
        }

        private void OnReset(Unit e)
        {
            TTarget[] buffer = _collectionB.ToArray();

            _collectionB.Clear();

            foreach (TTarget t in buffer)
            {
                _contract.Destroy(t);
            }
        }

        private void OnRemove(CollectionRemoveEvent<TSource> e)
        {
            TTarget removed = _collectionB[e.Index];

            _collectionB.RemoveAt(e.Index);

            _contract.Destroy(removed);
        }
    }
}