using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Fp.ObjectModels.Disposing;

using UniRx;

using Assert = UnityEngine.Assertions.Assert;

namespace Fp.DataBinding.Binds.Collections
{
    public sealed class CollectionBindScope<TValue> : DisposableObject, IReadOnlyList<TValue>
    {
        public const int CollectionWriteDepthLimit = 99;
        private int _size = 2;
        private CollectionBind<TValue>[] _binds = new CollectionBind<TValue>[2];
        private int[] _collectionLockers = new int[2];

        private readonly List<TValue> _commonArray = new List<TValue>();

        public int LastInitiatorCollectionIdx { get; private set; } = -1;

        public int LastInteractorCollectionIdx { get; private set; } = -1;

        #region IEnumerable Implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _commonArray).GetEnumerator();
        }

        #endregion

        #region IEnumerable<TValue> Implementation

        public IEnumerator<TValue> GetEnumerator()
        {
            return _commonArray.GetEnumerator();
        }

        #endregion

        #region IReadOnlyCollection<TValue> Implementation

        public int Count => _commonArray.Count;

        #endregion

        #region IReadOnlyList<TValue> Implementation

        public TValue this[int index] => _commonArray[index];

        #endregion

        public bool IsCollectionWriting(int collectionIdx)
        {
            return _collectionLockers[collectionIdx] > 0;
        }

        public bool IsCollectionReading(int collectionIdx)
        {
            return _collectionLockers[collectionIdx] < 0;
        }

        public bool IsCollectionIdle(int collectionIdx)
        {
            return _collectionLockers[collectionIdx] == 0;
        }

        public int GetCollectionWriteDepth(int collectionIdx)
        {
            return _collectionLockers[collectionIdx];
        }

        public int IndexOf(TValue value)
        {
            return _commonArray.IndexOf(value);
        }

        public int Register(CollectionBind<TValue> bind)
        {
            ThrowIfDisposedDebugOnly();

            int freePlace = Array.IndexOf(_binds, null);
            if (freePlace == -1)
            {
                _size *= 2;
                Array.Resize(ref _binds, _size);
                Array.Resize(ref _collectionLockers, _size);
                return Register(bind);
            }

            _binds[freePlace] = bind;
            return freePlace;
        }

        public void Unregister(int collectionIdx)
        {
            _binds[collectionIdx] = null;
            _collectionLockers[collectionIdx] = 0;
        }

        public void Insert(int fromIdx, CollectionAddEvent<TValue> collectionAddEvent)
        {
            ThrowIfDisposedDebugOnly();

            _commonArray.Insert(collectionAddEvent.Index, collectionAddEvent.Value);
            ExceptHandleWithOffset(fromIdx, c => c.ScopeInsert(collectionAddEvent));
        }

        public void Remove(int fromIdx, CollectionRemoveEvent<TValue> collectionRemoveEvent)
        {
            ThrowIfDisposedDebugOnly();

            Assert.AreEqual(_commonArray[collectionRemoveEvent.Index], collectionRemoveEvent.Value);

            _commonArray.RemoveAt(collectionRemoveEvent.Index);
            ExceptHandleWithOffset(fromIdx, c => c.ScopeRemove(collectionRemoveEvent));
        }

        public void Replace(int fromIdx, CollectionReplaceEvent<TValue> collectionReplaceEvent)
        {
            ThrowIfDisposedDebugOnly();

            _commonArray[collectionReplaceEvent.Index] = collectionReplaceEvent.NewValue;
            ExceptHandleWithOffset(fromIdx, c => c.ScopeReplace(collectionReplaceEvent));
        }

        public void Move(int fromIdx, CollectionMoveEvent<TValue> collectionMoveEvent)
        {
            ThrowIfDisposedDebugOnly();

            Assert.AreEqual(_commonArray[collectionMoveEvent.OldIndex], collectionMoveEvent.Value);
            _commonArray.RemoveAt(collectionMoveEvent.OldIndex);
            _commonArray[collectionMoveEvent.NewIndex] = collectionMoveEvent.Value;

            ExceptHandleWithOffset(fromIdx, c => c.ScopeMove(collectionMoveEvent));
        }

        public void Clear(int fromIdx)
        {
            ThrowIfDisposedDebugOnly();

            _commonArray.Clear();
            ExceptHandleWithOffset(fromIdx, c => c.ScopeReset());
        }

        protected override void OnDispose()
        {
            _binds = Array.Empty<CollectionBind<TValue>>();
            _commonArray.Clear();
            _size = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ExceptHandleWithOffset(int signalCollectionIdx, Action<ICollectionRestrictedBind<TValue>> action)
        {
            LastInteractorCollectionIdx = signalCollectionIdx;

            try
            {
                int changes = ++_collectionLockers[signalCollectionIdx];

                if (changes > CollectionWriteDepthLimit)
                {
                    throw new BindContextException(
                        $"Collection with index {signalCollectionIdx} overflowed depth limit");
                }

                int offset = signalCollectionIdx;
                for (var i = 1; i < _size; i++)
                {
                    int current = (i + offset) % _size;

                    int currentLock = _collectionLockers[current];
                    if (currentLock < 0)
                    {
                        if (currentLock < -1)
                        {
                            throw new BindContextException("Collection was changed from readonly access collection");
                        }

                        continue;
                    }

                    CollectionBind<TValue> collection = _binds[current];
                    if (collection == null)
                    {
                        continue;
                    }

                    try
                    {
                        _collectionLockers[current]--;
                        action(collection);
                    }
                    finally
                    {
                        _collectionLockers[current]++;
                    }
                }
            }
            finally
            {
                _collectionLockers[signalCollectionIdx]--;
                LastInitiatorCollectionIdx = signalCollectionIdx;
            }
        }
    }
}