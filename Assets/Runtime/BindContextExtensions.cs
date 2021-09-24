using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Fp.DataBinding.Binds;
using Fp.DataBinding.Binds.Collections;

using UniRx;

using UnityEngine.Assertions;

namespace Fp.DataBinding
{
    public static class BindContextExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Bind<TValue>(this BindContext ctx, string key, IObserver<TValue> observer, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Bind(key, observer).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(this BindContext ctx, string key, IObserver<TValue> observer)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(observer);

            uint id = BindContext.GetHashKey(key);
            return ctx.CreateBind(id, null, observer, RelationType.FromSource);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(this BindContext ctx, uint id, IObserver<TValue> observer)
        {
            Assert.IsNotNull(observer);

            return ctx.CreateBind(id, null, observer, RelationType.FromSource);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Bind<TValue>(this BindContext ctx, string key, IObservable<TValue> observable, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Bind(key, observable).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(this BindContext ctx, string key, IObservable<TValue> observable)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(observable);

            uint id = BindContext.GetHashKey(key);
            return ctx.CreateBind(id, observable, null, RelationType.OneWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(this BindContext ctx, uint id, IObservable<TValue> observable)
        {
            Assert.IsNotNull(observable);

            return ctx.CreateBind(id, observable, null, RelationType.OneWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Bind<TValue>(
            this BindContext ctx,
            string key,
            IObservable<TValue> observable,
            IObserver<TValue> observer,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Bind(key,
                     observable,
                     observer).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(
            this BindContext ctx,
            string key,
            IObservable<TValue> observable,
            IObserver<TValue> observer)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(observable);
            Assert.IsNotNull(observer);
            uint id = BindContext.GetHashKey(key);
            return ctx.CreateBind(id, observable, observer, RelationType.TwoWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(
            this BindContext ctx,
            uint id,
            IObservable<TValue> observable,
            IObserver<TValue> observer)
        {
            Assert.IsNotNull(observable);
            Assert.IsNotNull(observer);

            return ctx.CreateBind(id, observable, observer, RelationType.TwoWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Bind<TValue>(
            this BindContext ctx,
            string key,
            IReactiveProperty<TValue> target,
            RelationType relation,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Bind(key,
                     target,
                     relation = RelationType.TwoWay).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(
            this BindContext ctx,
            string key,
            IReactiveProperty<TValue> target,
            RelationType relation = RelationType.TwoWay)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(target);

            uint id = BindContext.GetHashKey(key);
            return ctx.CreateBind(id, target, Observer.Create<TValue>(value => target.Value = value, () =>
            {
                if (target is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }), relation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Bind<TValue>(
            this BindContext ctx,
            uint id,
            IReactiveProperty<TValue> target,
            RelationType relation = RelationType.TwoWay)
        {
            Assert.IsNotNull(target);

            return ctx.CreateBind(id, target, Observer.Create<TValue>(value => target.Value = value, () =>
            {
                if (target is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }), relation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BindCollection<TValue>(
            this BindContext ctx,
            string key,
            IReactiveCollection<TValue> target,
            CollectionMargeOption marge,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.BindCollection(key,
                               target,
                               marge).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable BindCollection<TValue>(
            this BindContext ctx,
            string key,
            IReactiveCollection<TValue> target,
            CollectionMargeOption marge = CollectionMargeOption.ReplaceCollection)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            uint id = BindContext.GetHashKey(key);
            return ctx.CreateCollectionBind(id, target, marge);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable BindCollection<TValue>(
            this BindContext ctx,
            uint id,
            IReactiveCollection<TValue> target,
            CollectionMargeOption marge = CollectionMargeOption.ReplaceCollection)
        {
            return ctx.CreateCollectionBind(id, target, marge);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool BindDictionary<TKey, TValue>(
            this BindContext ctx,
            string key,
            IReactiveDictionary<TKey, TValue> target,
            CollectionMargeOption marge,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.BindDictionary(key, target, marge).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable BindDictionary<TKey, TValue>(
            this BindContext ctx,
            string key,
            IReactiveDictionary<TKey, TValue> target,
            CollectionMargeOption marge = CollectionMargeOption.ReplaceCollection)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            uint id = BindContext.GetHashKey(key);
            return ctx.CreateDictionaryBind(id, target, marge);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable BindDictionary<TKey, TValue>(
            this BindContext ctx,
            uint id,
            IReactiveDictionary<TKey, TValue> target,
            CollectionMargeOption marge = CollectionMargeOption.ReplaceCollection)
        {
            return ctx.CreateDictionaryBind(id, target, marge);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CollectionContract<TSource, TTarget>(
            this BindContext ctx,
            string key,
            IConvertContract<TSource, TTarget> contract,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.CollectionContract(key, contract).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable CollectionContract<TSource, TTarget>(this BindContext ctx, string key, IConvertContract<TSource, TTarget> contract)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            uint id = BindContext.GetHashKey(key);
            return ctx.MakeCollectionContract(id, id, contract);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable CollectionContract<TSource, TTarget>(this BindContext ctx, uint id, IConvertContract<TSource, TTarget> contract)
        {
            Assert.AreNotEqual(typeof(TSource), typeof(TTarget), "Contract type must be different when using with same id");
            return ctx.MakeCollectionContract(id, id, contract);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CollectionToSubContextContract<TSource>(this BindContext ctx, string key, ICollection<IDisposable> disposable)
            where TSource : IDataContext
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.CollectionToSubContextContract<TSource>(key).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable CollectionToSubContextContract<TSource>(this BindContext ctx, string key)
            where TSource : IDataContext
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            var sourceToBindContext = new DataContextConverter<TSource>(ctx);
            return CollectionContract(ctx, key, sourceToBindContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable CollectionToSubContextContract<TSource>(this BindContext ctx, uint id)
            where TSource : IDataContext
        {
            var sourceToBindContext = new DataContextConverter<TSource>(ctx);
            return CollectionContract(ctx, id, sourceToBindContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DictionaryContract<TKey, TSource, TTarget>(
            this BindContext ctx,
            string key,
            IConvertContract<TSource, TTarget> sourceToTargetContract,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.DictionaryContract<TKey, TSource, TTarget>(key,
                                                           sourceToTargetContract).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable DictionaryContract<TKey, TSource, TTarget>(
            this BindContext ctx,
            string key,
            IConvertContract<TSource, TTarget> sourceToTargetContract)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            uint id = BindContext.GetHashKey(key);
            return ctx.DictionaryContract<TKey, TSource, TTarget>(id, sourceToTargetContract);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable DictionaryContract<TKey, TSource, TTarget>(
            this BindContext ctx,
            uint id,
            IConvertContract<TSource, TTarget> sourceToTargetContract)
        {
            return ctx.MakeDictionaryContract<TKey, TSource, TTarget>(id, id, sourceToTargetContract);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DictionaryToSubContextContract<TKey, TSource>(this BindContext ctx, string key, ICollection<IDisposable> disposable)
            where TSource : IDataContext
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.DictionaryToSubContextContract<TKey, TSource>(key).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable DictionaryToSubContextContract<TKey, TSource>(this BindContext ctx, string key)
            where TSource : IDataContext
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            uint id = BindContext.GetHashKey(key);
            return DictionaryToSubContextContract<TKey, TSource>(ctx, id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable DictionaryToSubContextContract<TKey, TSource>(this BindContext ctx, uint id)
            where TSource : IDataContext
        {
            var sourceToBindContext = new DataContextConverter<TSource>(ctx);
            return DictionaryContract<TKey, TSource, BindContext>(ctx, id, sourceToBindContext);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Signal<T>(this BindContext ctx, string key, Action<T> triggerAction, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Signal(key, triggerAction).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Signal<T>(this BindContext ctx, string key, Action<T> triggerAction)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(triggerAction);

            uint id = BindContext.GetHashKey(key);
            return ctx.Signal(id, triggerAction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Signal<T>(this BindContext ctx, uint id, Action<T> triggerAction)
        {
            Assert.IsNotNull(triggerAction);

            return ctx.CreateCommandBind(id, null, Observer.Create(triggerAction), RelationType.FromSource);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Signal(this BindContext ctx, string key, Action triggerAction, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Signal(key, triggerAction).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Signal(this BindContext ctx, string key, Action triggerAction)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(triggerAction);

            return ctx.Signal<Unit>(key, u => triggerAction?.Invoke());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Signal(this BindContext ctx, uint id, Action triggerAction)
        {
            Assert.IsNotNull(triggerAction);

            return ctx.Signal<Unit>(id, u => triggerAction?.Invoke());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Trigger<T>(this BindContext ctx, string key, IObservable<T> observable, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Trigger(key, observable).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Trigger<T>(this BindContext ctx, string key, IObservable<T> observable)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(observable);

            uint id = BindContext.GetHashKey(key);
            return ctx.Trigger(id, observable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Trigger<T>(this BindContext ctx, uint id, IObservable<T> observable)
        {
            Assert.IsNotNull(observable);

            return ctx.CreateCommandBind(id, observable, null, RelationType.OneWay);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Trigger(this BindContext ctx, string key, IObservable<Unit> observable, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Trigger(key, observable).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Trigger(this BindContext ctx, string key, IObservable<Unit> observable)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(observable);

            return ctx.Trigger<Unit>(key, observable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Trigger(this BindContext ctx, uint id, IObservable<Unit> observable)
        {
            Assert.IsNotNull(observable);

            return ctx.Trigger<Unit>(id, observable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Trigger<T>(
            this BindContext ctx,
            string key,
            IReactiveCommand<T> command,
            RelationType relation,
            ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.Trigger(key,
                        command,
                        relation).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Trigger<T>(
            this BindContext ctx,
            string key,
            IReactiveCommand<T> command,
            RelationType relation)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            Assert.IsNotNull(command);

            uint id = BindContext.GetHashKey(key);
            return ctx.Trigger(id, command, relation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable Trigger<T>(
            this BindContext ctx,
            uint id,
            IReactiveCommand<T> command,
            RelationType relation)
        {
            Assert.IsNotNull(command);

            return ctx.CreateCommandBind(id, command, Observer.Create<T>(v => command.Execute(v)), relation);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TextData(this BindContext ctx, string key, IObservable<string> observable, ICollection<IDisposable> disposable)
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.TextData(key, observable).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable TextData(this BindContext ctx, string key, IObservable<string> observable)
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");
            return TextData(ctx, key, observable.Select(s => new FormattableString(s)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable TextData(this BindContext ctx, uint id, IObservable<string> observable)
        {
            return TextData(ctx, id, observable.Select(s => new FormattableString(s)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TextData<T>(this BindContext ctx, string key, IObservable<T> observable, ICollection<IDisposable> disposable)
            where T : IFormattable
        {
            if (!IsValidKey(key))
            {
                return false;
            }

            ctx.TextData(key, observable).AddTo(disposable);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable TextData<T>(this BindContext ctx, string key, IObservable<T> observable)
            where T : IFormattable
        {
            Assert.IsTrue(IsValidKey(key), "IsValidKey(key)");

            uint id = BindContext.GetHashKey(key);
            return ctx.TextData(id, observable);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable TextData<T>(this BindContext ctx, uint id, IObservable<T> observable)
            where T : IFormattable
        {
            return ctx.CreateBind(id, observable.Select(value => (IFormattable) value), null, RelationType.OneWay);
            ;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidKey(string key)
        {
            return !string.IsNullOrWhiteSpace(key);
        }

        private struct FormattableString : IFormattable
        {
            private readonly string _value;

            public FormattableString(string value)
            {
                _value = value ?? string.Empty;
            }

            #region IFormattable Implementation

            public string ToString(string format, IFormatProvider formatProvider)
            {
                return _value.ToString(formatProvider);
            }

            #endregion
        }
    }
}