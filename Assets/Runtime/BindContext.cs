using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Fp.DataBinding.Binds;
using Fp.DataBinding.Binds.Collections;
using Fp.DataBinding.Binds.Collections.Contracts;
using Fp.Pooling;
using Fp.Utility.Cryptography;

using UniRx;

using Debug = UnityEngine.Debug;

namespace Fp.DataBinding
{
    public class BindContext
    {
        private readonly TypedCollection<IDisposable> _binds = new TypedCollection<IDisposable>();

        private BindContext _parent;

        private readonly Subject<Unit> _finalizeSubject = new Subject<Unit>();

        static BindContext()
        {
            Pool = new ObjectPoolStrategy<BindContext>(DataContextFactory, Finalize);
        }

        public static ObjectPoolStrategy<BindContext> Pool { get; }

        public IObservable<Unit> WhenFinalize()
        {
            return _finalizeSubject.Take(1);
        }

        public BindContext SubContext()
        {
            return Pool.Rent().SetParent(this);
        }

        public bool ReturnContext(BindContext context)
        {
            if (context._parent == this)
            {
                context._finalizeSubject.OnNext(Unit.Default);
                Pool.Return(context);
                return true;
            }

            return false;
        }

        [Conditional("DEBUG_DATA_BINDING")]
        private static void LogInfo(string message)
        {
            Debug.Log($"[Data Binding] {message}");
        }

        public ValueBind<T> CreateBind<T>(
            uint id,
            IObservable<T> observable,
            IObserver<T> observer,
            RelationType relation)
        {
            if (TryGetBind(id, out ReactiveValue<T> property))
            {
                return new ValueBind<T>(property, observable, observer, relation);
            }

            property = new ReactiveValue<T>();

            _binds.Add<ReactiveValue<T>>(id, property);

            LogInfo($"Bind Context created {typeof(ValueBind<T>)} with key {id:X}");
            return new ValueBind<T>(property, observable, observer, relation);
        }

        public CommandBind<T> CreateCommandBind<T>(
            uint id,
            IObservable<T> observable,
            IObserver<T> observer,
            RelationType relation)
        {
            if (!TryGetBind(id, out ReactiveCommand<T> property))
            {
                property = new ReactiveCommand<T>();
                _binds.Add<ReactiveCommand<T>>(id, property);
            }

            return new CommandBind<T>(property, observable, observer, relation);
        }

        public ICollectionBind CreateCollectionBind<TValue>(
            uint id,
            IReactiveCollection<TValue> target,
            CollectionMargeOption marge = CollectionMargeOption.ReplaceCollection)
        {
            if (!TryGetBind(id, out CollectionBindScope<TValue> reactiveCollection))
            {
                reactiveCollection = new CollectionBindScope<TValue>();
                _binds.Add<CollectionBindScope<TValue>>(id, reactiveCollection);
                LogInfo($"Collection Bind scope created {typeof(CollectionBindScope<TValue>)} with key {id:X}");
            }

            return new CollectionBind<TValue>(reactiveCollection, target, marge);
        }

        public ICollectionBind CreateDictionaryBind<TKey, TValue>(
            uint id,
            IReactiveDictionary<TKey, TValue> target,
            CollectionMargeOption marge = CollectionMargeOption.ReplaceCollection)
        {
            if (!TryGetBind(id, out DictionaryBindScope<TKey, TValue> reactiveDictionary))
            {
                reactiveDictionary = new DictionaryBindScope<TKey, TValue>();
                _binds.Add<DictionaryBindScope<TKey, TValue>>(id, reactiveDictionary);

                LogInfo($"Dictionary Bind scope created {typeof(DictionaryBindScope<TKey, TValue>)} with key {id:X}");
            }

            return new DictionaryBind<TKey, TValue>(reactiveDictionary, target, marge);
        }

        public IDisposable MakeCollectionContract<TSource, TTarget>(
            uint sourceId,
            uint targetId,
            IConvertContract<TSource, TTarget> contract)
        {
            if (sourceId == targetId && typeof(TSource) == typeof(TTarget))
            {
                throw new BindContextException($"The passed collections(contractors) mustn't be same. SourceId: {sourceId}, TargetID: {targetId}");
            }

            return new CollectionContractBridge<TSource, TTarget>(sourceId, targetId, this, contract);
        }

        public IDisposable MakeDictionaryContract<TKey, TSource, TTarget>(
            uint sourceId,
            uint targetId,
            IConvertContract<TSource, TTarget> contract)
        {
            if (sourceId == targetId && typeof(TSource) == typeof(TTarget))
            {
                throw new BindContextException($"The passed dictionary(contractors) mustn't be same. SourceId: {sourceId}, TargetID: {targetId}");
            }

            return new DictionaryContractBridge<TKey, TSource, TTarget>(sourceId, targetId, this, contract);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint GetHashKey(string key)
        {
            return HashUtility.GetHashKeyCached(key);
        }

        private BindContext SetParent(BindContext bindContext)
        {
            _parent = bindContext;

            return this;
        }

        private static void Finalize(BindContext bindCtx)
        {
            foreach (IDisposable disposable in bindCtx._binds)
            {
                disposable.Dispose();
            }

            bindCtx._binds.Clear();
            bindCtx._parent = null;
        }

        private static BindContext DataContextFactory()
        {
            return new BindContext();
        }

        private bool TryGetBind<T>(uint key, out T bind)
            where T : IDisposable
        {
            if (_binds.TryGetValue(key, out bind))
            {
                return true;
            }

            if (_parent != null)
            {
                if (!_parent.TryGetBind(key, out bind))
                {
                    return _binds.TryGetValue(key, out bind);
                }
            }

            bind = default;
            return false;
        }
    }
}