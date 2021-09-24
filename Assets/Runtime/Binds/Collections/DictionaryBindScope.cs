using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Fp.ObjectModels.Disposing;

using UniRx;

using Assert = UnityEngine.Assertions.Assert;

namespace Fp.DataBinding.Binds.Collections
{
    public sealed class DictionaryBindScope<TKey, TValue> : DisposableObject, IReadOnlyDictionary<TKey, TValue>
    {
        public const int CollectionWriteDepthLimit = 99;
        private int _size = 2;
        private DictionaryBind<TKey, TValue>[] _binds = new DictionaryBind<TKey, TValue>[2];
        private int[] _dictionaryLockers = new int[2];

        private readonly Dictionary<TKey, TValue> _commonDictionary = new Dictionary<TKey, TValue>();

        public int LastInitiatorDictionaryIdx { get; private set; } = -1;

        public int LastInteractorDictionaryIdx { get; private set; } = -1;

        #region IEnumerable Implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Implementation

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _commonDictionary.GetEnumerator();
        }

        #endregion

        #region IReadOnlyCollection<KeyValuePair<TKey,TValue>> Implementation

        public int Count => _commonDictionary.Count;

        #endregion

        #region IReadOnlyDictionary<TKey,TValue> Implementation

        public IEnumerable<TKey> Keys => _commonDictionary.Keys;
        public IEnumerable<TValue> Values => _commonDictionary.Values;

        public bool ContainsKey(TKey key)
        {
            return _commonDictionary.ContainsKey(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _commonDictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key] => _commonDictionary[key];

        #endregion

        public bool IsDictionaryWriting(int collectionIdx)
        {
            return _dictionaryLockers[collectionIdx] > 0;
        }

        public bool IsDictionaryReading(int collectionIdx)
        {
            return _dictionaryLockers[collectionIdx] < 0;
        }

        public bool IsDictionaryIdle(int collectionIdx)
        {
            return _dictionaryLockers[collectionIdx] == 0;
        }

        public int GetDictionaryWriteDepth(int collectionIdx)
        {
            return _dictionaryLockers[collectionIdx];
        }

        public int Register(DictionaryBind<TKey, TValue> bind)
        {
            ThrowIfDisposedDebugOnly();

            int freePlace = Array.IndexOf(_binds, null);
            if (freePlace == -1)
            {
                _size *= 2;
                Array.Resize(ref _binds, _size);
                Array.Resize(ref _dictionaryLockers, _size);
                return Register(bind);
            }

            _binds[freePlace] = bind;
            return freePlace;
        }

        public void Unregister(int collectionIdx)
        {
            _binds[collectionIdx] = null;
            _dictionaryLockers[collectionIdx] = 0;
        }

        public void Add(int fromIdx, DictionaryAddEvent<TKey, TValue> dictionaryAddEvent)
        {
            ThrowIfDisposedDebugOnly();
            _commonDictionary.Add(dictionaryAddEvent.Key, dictionaryAddEvent.Value);

            ExceptHandleWithOffset(fromIdx, c => c.ScopeAdd(dictionaryAddEvent));
        }

        public void Remove(int fromIdx, DictionaryRemoveEvent<TKey, TValue> dictionaryRemoveEvent)
        {
            ThrowIfDisposedDebugOnly();
            Assert.AreEqual(_commonDictionary[dictionaryRemoveEvent.Key], dictionaryRemoveEvent.Value);
            _commonDictionary.Remove(dictionaryRemoveEvent.Key);

            ExceptHandleWithOffset(fromIdx, c => c.ScopeRemove(dictionaryRemoveEvent));
        }

        public void Replace(int fromIdx, DictionaryReplaceEvent<TKey, TValue> dictionaryReplaceEvent)
        {
            ThrowIfDisposedDebugOnly();
            _commonDictionary[dictionaryReplaceEvent.Key] = dictionaryReplaceEvent.NewValue;

            ExceptHandleWithOffset(fromIdx, c => c.ScopeReplace(dictionaryReplaceEvent));
        }

        public void Clear(int fromIdx)
        {
            ThrowIfDisposedDebugOnly();

            _commonDictionary.Clear();
            ExceptHandleWithOffset(fromIdx, c => c.ScopeReset());
        }

        protected override void OnDispose()
        {
            _binds = Array.Empty<DictionaryBind<TKey, TValue>>();
            _commonDictionary.Clear();
            _size = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExceptHandleWithOffset(
            int signalDictionaryIdx,
            Action<IDictionaryRestrictedBind<TKey, TValue>> action)
        {
            LastInteractorDictionaryIdx = signalDictionaryIdx;

            try
            {
                int changes = ++_dictionaryLockers[signalDictionaryIdx];

                if (changes > CollectionWriteDepthLimit)
                {
                    throw new BindContextException(
                        $"Dictionary with index {signalDictionaryIdx} overflowed depth limit");
                }

                int offset = signalDictionaryIdx;
                for (var i = 1; i < _size; i++)
                {
                    int current = (i + offset) % _size;

                    int currentLock = _dictionaryLockers[current];
                    if (currentLock < 0)
                    {
                        if (currentLock < -1)
                        {
                            throw new BindContextException("Dictionary was changed from readonly access dictionary");
                        }

                        continue;
                    }

                    DictionaryBind<TKey, TValue> dictionary = _binds[current];
                    if (dictionary == null)
                    {
                        continue;
                    }

                    try
                    {
                        _dictionaryLockers[current]--;
                        action(dictionary);
                    }
                    finally
                    {
                        _dictionaryLockers[current]++;
                    }
                }
            }
            finally
            {
                _dictionaryLockers[signalDictionaryIdx]--;
                LastInitiatorDictionaryIdx = signalDictionaryIdx;
            }
        }
    }
}