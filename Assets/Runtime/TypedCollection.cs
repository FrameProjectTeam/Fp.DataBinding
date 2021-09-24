using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Fp.DataBinding
{
    public class TypedCollection<TBaseImpl> : IEnumerable<TBaseImpl>
    {
        private readonly Dictionary<Type, Dictionary<uint, TBaseImpl>> _dataBindByType;

        public TypedCollection()
        {
            _dataBindByType = new Dictionary<Type, Dictionary<uint, TBaseImpl>>();
        }

        #region IEnumerable Implementation

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IEnumerable<TBaseImpl> Implementation

        public IEnumerator<TBaseImpl> GetEnumerator()
        {
            return _dataBindByType.Values.SelectMany(v => v.Values).GetEnumerator();
        }

        #endregion

        public void Clear()
        {
            foreach (Dictionary<uint, TBaseImpl> dictionary in _dataBindByType.Values)
            {
                dictionary.Clear();
            }
        }

        public bool TryGetValue<TValue>(uint key, out TValue result)
            where TValue : TBaseImpl
        {
            Dictionary<uint, TBaseImpl> target = GetDictionary(typeof(TValue));

            if (target.TryGetValue(key, out TBaseImpl contains))
            {
                result = (TValue) contains;
                return true;
            }

            result = default;
            return false;
        }

        public void Replace<TValue>(uint key, TBaseImpl value)
        {
            Dictionary<uint, TBaseImpl> dictionary = GetDictionary(typeof(TValue));
            dictionary[key] = value;
        }

        public void Add<TValue>(uint key, TBaseImpl value)
        {
            Dictionary<uint, TBaseImpl> dictionary = GetDictionary(typeof(TValue));
            dictionary.Add(key, value);
        }

        public bool Remove<TValue>(uint key)
        {
            Dictionary<uint, TBaseImpl> dictionary = GetDictionary(typeof(TValue));
            return dictionary.Remove(key);
        }

        private Dictionary<uint, TBaseImpl> GetDictionary(Type type)
        {
            if (_dataBindByType.TryGetValue(type, out Dictionary<uint, TBaseImpl> bindDictionary))
            {
                return bindDictionary;
            }

            bindDictionary = new Dictionary<uint, TBaseImpl>();
            _dataBindByType[type] = bindDictionary;

            return bindDictionary;
        }
    }
}