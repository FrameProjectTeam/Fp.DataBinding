using System.Collections.Generic;

using Fp.ObjectModels.Disposing;

using UniRx;

namespace Fp.DataBinding.Binds.Collections
{
    public sealed class DictionaryBind<TKey, TValue> : DisposableObject, IDictionaryRestrictedBind<TKey, TValue>
    {
        private readonly DictionaryBindScope<TKey, TValue> _scope;
        private readonly IReactiveDictionary<TKey, TValue> _boundDictionary;

        private readonly CompositeDisposable _relativeResources = new CompositeDisposable();

        private int _scopeIdx;

        private readonly Stack<DictionaryAddEvent<TKey, TValue>> _addingStack =
            new Stack<DictionaryAddEvent<TKey, TValue>>(1);

        private readonly Stack<DictionaryRemoveEvent<TKey, TValue>> _removingStack =
            new Stack<DictionaryRemoveEvent<TKey, TValue>>(1);

        private readonly Stack<DictionaryReplaceEvent<TKey, TValue>> _replacingStack =
            new Stack<DictionaryReplaceEvent<TKey, TValue>>(1);

        private int _resetting;

        public DictionaryBind(
            DictionaryBindScope<TKey, TValue> scope,
            IReactiveDictionary<TKey, TValue> boundDictionary,
            CollectionMargeOption margeOption = CollectionMargeOption.ReplaceCollection)
        {
            _scope = scope;
            _boundDictionary = boundDictionary;

            Init(margeOption);
        }

        private IDictionary<TKey, TValue> BoundDictionary => _boundDictionary;

        #region ICollectionBind Implementation

        public bool IsWriting => _scope.IsDictionaryWriting(_scopeIdx);
        public bool IsReading => _scope.IsDictionaryReading(_scopeIdx);
        public bool IsActive => !_scope.IsDictionaryIdle(_scopeIdx);

        public int ActiveDepth => _scope.GetDictionaryWriteDepth(_scopeIdx);
        public bool IsLastInitiator => _scope.LastInitiatorDictionaryIdx == _scopeIdx;
        public bool IsLastInteractor => _scope.LastInteractorDictionaryIdx == _scopeIdx;

        #endregion

        #region IDictionaryRestrictedBind<TKey,TValue> Implementation

        void IDictionaryRestrictedBind<TKey, TValue>.ScopeReset()
        {
            _resetting++;
            _boundDictionary.Clear();
        }

        void IDictionaryRestrictedBind<TKey, TValue>.ScopeAdd(DictionaryAddEvent<TKey, TValue> dictionaryAddEvent)
        {
            _addingStack.Push(dictionaryAddEvent);
            _boundDictionary.Add(dictionaryAddEvent.Key, dictionaryAddEvent.Value);
        }

        void IDictionaryRestrictedBind<TKey, TValue>.ScopeRemove(
            DictionaryRemoveEvent<TKey, TValue> dictionaryRemoveEvent)
        {
            _removingStack.Push(dictionaryRemoveEvent);
            _boundDictionary.Remove(dictionaryRemoveEvent.Key);
        }

        void IDictionaryRestrictedBind<TKey, TValue>.ScopeReplace(
            DictionaryReplaceEvent<TKey, TValue> dictionaryReplaceEvent)
        {
            _replacingStack.Push(dictionaryReplaceEvent);
            BoundDictionary[dictionaryReplaceEvent.Key] = dictionaryReplaceEvent.NewValue;
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

            if (scopeCount > 0 || scopeCount > 0)
            {
                switch (margeOption)
                {
                    case CollectionMargeOption.ReplaceCollection:
                    {
                        _boundDictionary.Clear();
                        foreach (KeyValuePair<TKey, TValue> kvp in _scope)
                        {
                            _boundDictionary.Add(kvp.Key, kvp.Value);
                        }

                        break;
                    }

                    case CollectionMargeOption.ReplaceScope:
                    {
                        _scope.Clear(_scopeIdx);
                        foreach (KeyValuePair<TKey, TValue> kvp in _boundDictionary)
                        {
                            _scope.Add(_scopeIdx, new DictionaryAddEvent<TKey, TValue>(kvp.Key, kvp.Value));
                        }

                        break;
                    }

                    case CollectionMargeOption.Merge:
                    {
                        if (BoundDictionary.Count == 0)
                        {
                            break;
                        }

                        foreach (KeyValuePair<TKey, TValue> kvp in _boundDictionary)
                        {
                            if (_scope.ContainsKey(kvp.Key))
                            {
                                continue;
                            }

                            _scope.Add(_scopeIdx, new DictionaryAddEvent<TKey, TValue>(kvp.Key, kvp.Value));
                        }

                        _boundDictionary.Clear();
                        foreach (KeyValuePair<TKey, TValue> kvp in _scope)
                        {
                            _boundDictionary.Add(kvp.Key, kvp.Value);
                        }

                        break;
                    }
                }
            }

            //Source collection to bind collection
            _boundDictionary.ObserveAdd().Subscribe(OnBoundAdd).AddTo(_relativeResources);
            _boundDictionary.ObserveReset().Subscribe(OnBoundReset).AddTo(_relativeResources);
            _boundDictionary.ObserveRemove().Subscribe(OnBoundRemove).AddTo(_relativeResources);
            _boundDictionary.ObserveReplace().Subscribe(OnBoundReplace).AddTo(_relativeResources);
        }

        private void OnBoundAdd(DictionaryAddEvent<TKey, TValue> e)
        {
            if (_addingStack.Count > 0 && _addingStack.Peek().Equals(e))
            {
                _addingStack.Pop();
            }
            else
            {
                _scope.Add(_scopeIdx, e);
            }
        }

        private void OnBoundReplace(DictionaryReplaceEvent<TKey, TValue> e)
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

        private void OnBoundRemove(DictionaryRemoveEvent<TKey, TValue> e)
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

        private void OnBoundReset(Unit obj)
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