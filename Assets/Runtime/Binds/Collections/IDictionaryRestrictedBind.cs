using UniRx;

namespace Fp.DataBinding.Binds.Collections
{
    internal interface IDictionaryRestrictedBind<TKey, TValue> : ICollectionBind
    {
        void ScopeAdd(DictionaryAddEvent<TKey, TValue> dictionaryAddEvent);
        void ScopeRemove(DictionaryRemoveEvent<TKey, TValue> dictionaryRemoveEvent);
        void ScopeReplace(DictionaryReplaceEvent<TKey, TValue> dictionaryReplaceEvent);
        void ScopeReset();
    }
}