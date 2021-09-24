using System;
using System.Collections.Generic;

using Fp.ObjectModels.Disposing;

using UniRx;

namespace Fp.DataBinding.Binds.Collections
{
    public sealed class CollectionBind<TValue> : DisposableObject, ICollectionRestrictedBind<TValue>
    {
        private readonly CollectionBindScope<TValue> _scope;
        private readonly IReactiveCollection<TValue> _boundCollection;

        private readonly CompositeDisposable _relativeResources = new CompositeDisposable();

        private int _scopeIdx;

        private readonly Stack<CollectionAddEvent<TValue>> _addingStack =
            new Stack<CollectionAddEvent<TValue>>(1);

        private readonly Stack<CollectionRemoveEvent<TValue>> _removingStack =
            new Stack<CollectionRemoveEvent<TValue>>(1);

        private readonly Stack<CollectionReplaceEvent<TValue>> _replacingStack =
            new Stack<CollectionReplaceEvent<TValue>>(1);

        private readonly Stack<CollectionMoveEvent<TValue>> _movingStack =
            new Stack<CollectionMoveEvent<TValue>>(1);

        private int _resetting;

        public CollectionBind(
            CollectionBindScope<TValue> scope,
            IReactiveCollection<TValue> boundCollection,
            CollectionMargeOption margeOption = CollectionMargeOption.ReplaceCollection)
        {
            _scope = scope;
            _boundCollection = boundCollection;

            Init(margeOption);
        }

        #region ICollectionBind Implementation

        public bool IsWriting => _scope.IsCollectionWriting(_scopeIdx);
        public bool IsReading => _scope.IsCollectionReading(_scopeIdx);
        public bool IsActive => !_scope.IsCollectionIdle(_scopeIdx);

        public int ActiveDepth => _scope.GetCollectionWriteDepth(_scopeIdx);

        public bool IsLastInitiator => _scope.LastInitiatorCollectionIdx == _scopeIdx;
        public bool IsLastInteractor => _scope.LastInteractorCollectionIdx == _scopeIdx;

        #endregion

        #region ICollectionRestrictedBind<TValue> Implementation

        public void ScopeInsert(CollectionAddEvent<TValue> collectionAddEvent)
        {
            _addingStack.Push(collectionAddEvent);
            _boundCollection.Insert(collectionAddEvent.Index, collectionAddEvent.Value);
        }

        void ICollectionRestrictedBind<TValue>.ScopeRemove(CollectionRemoveEvent<TValue> collectionRemoveEvent)
        {
            _removingStack.Push(collectionRemoveEvent);
            _boundCollection.RemoveAt(collectionRemoveEvent.Index);
        }

        void ICollectionRestrictedBind<TValue>.ScopeReplace(CollectionReplaceEvent<TValue> collectionReplaceEvent)
        {
            _replacingStack.Push(collectionReplaceEvent);
            _boundCollection[collectionReplaceEvent.Index] = collectionReplaceEvent.NewValue;
        }

        void ICollectionRestrictedBind<TValue>.ScopeMove(CollectionMoveEvent<TValue> collectionMoveEvent)
        {
            _movingStack.Push(collectionMoveEvent);
            _boundCollection.Move(collectionMoveEvent.OldIndex, collectionMoveEvent.NewIndex);
        }

        void ICollectionRestrictedBind<TValue>.ScopeReset()
        {
            _resetting++;
            _boundCollection.Clear();
        }

        #endregion

        protected override void OnDispose()
        {
            _scope.Unregister(_scopeIdx);
            _relativeResources.Dispose();
        }

        private void Init(CollectionMargeOption margeOption)
        {
            _scopeIdx = _scope.Register(this);

            int scopeCount = _scope.Count;

            if (scopeCount > 0 || _boundCollection.Count > 0)
            {
                switch (margeOption)
                {
                    case CollectionMargeOption.ReplaceCollection:
                    {
                        _boundCollection.Clear();
                        foreach (TValue value in _scope)
                        {
                            _boundCollection.Add(value);
                        }

                        //Check if they are still similar.
                        if (_boundCollection.Count == scopeCount)
                        {
                            var validateCount = 0;
                            for (var i = 0; i < scopeCount; i++)
                            {
                                if (EqualityComparer<TValue>.Default.Equals(_scope[i], _boundCollection[i]))
                                {
                                    validateCount++;
                                }
                            }

                            if (validateCount != scopeCount)
                            {
                                throw new FailedSyncCollectionException(
                                    "Failed validation objects one or more objects in bound collection isn't same with original, but must be same.");
                            }
                        }
                        else
                        {
                            throw new FailedSyncCollectionException("Bound collection has different element count");
                        }

                        break;
                    }
                    case CollectionMargeOption.ReplaceScope:
                    {
                        _scope.Clear(_scopeIdx);
                        for (var i = 0; i < _boundCollection.Count; i++)
                        {
                            _scope.Insert(_scopeIdx, new CollectionAddEvent<TValue>(i, _boundCollection[i]));
                        }

                        break;
                    }
                    default:
                    {
                        throw new NotImplementedException();
                    }
                }
            }

            //Source collection to bind collection
            _boundCollection.ObserveAdd().Subscribe(OnCollectionAdded).AddTo(_relativeResources);
            _boundCollection.ObserveRemove().Subscribe(OnCollectionRemoved).AddTo(_relativeResources);
            _boundCollection.ObserveReplace().Subscribe(OnCollectionReplaced).AddTo(_relativeResources);
            _boundCollection.ObserveMove().Subscribe(OnCollectionMoved).AddTo(_relativeResources);
            _boundCollection.ObserveReset().Subscribe(OnCollectionReset).AddTo(_relativeResources);
        }

        private void OnCollectionAdded(CollectionAddEvent<TValue> e)
        {
            if (_addingStack.Count > 0 && _addingStack.Peek().Equals(e))
            {
                _addingStack.Pop();
            }
            else
            {
                _scope.Insert(_scopeIdx, e);
            }
        }

        private void OnCollectionRemoved(CollectionRemoveEvent<TValue> e)
        {
            if (_removingStack.Count > 0 && _removingStack.Peek().Equals(e))
            {
                _removingStack.Pop();
            }
            else
            {
                _scope.Remove(_scopeIdx, e);
            }
        }

        private void OnCollectionReplaced(CollectionReplaceEvent<TValue> e)
        {
            if (_replacingStack.Count > 0 && _replacingStack.Peek().Equals(e))
            {
                _replacingStack.Pop();
            }
            else
            {
                _scope.Replace(_scopeIdx, e);
            }
        }

        private void OnCollectionMoved(CollectionMoveEvent<TValue> e)
        {
            if (_movingStack.Count > 0 && _movingStack.Peek().Equals(e))
            {
                _movingStack.Pop();
            }
            else
            {
                _scope.Move(_scopeIdx, e);
            }
        }

        private void OnCollectionReset(Unit e)
        {
            if (_resetting > 0)
            {
                _resetting--;
            }
            else
            {
                _scope.Clear(_scopeIdx);
            }
        }
    }
}