using System;

namespace Fp.DataBinding.Binds
{
    public sealed class FunctorConverter<TContract, TTarget> : IConvertContract<TContract, TTarget>
    {
        private readonly Func<TContract, TTarget> _create;
        private readonly Action<TTarget> _destroy;

        public FunctorConverter(Func<TContract, TTarget> create, Action<TTarget> destroy)
        {
            _create = create;

            _destroy = destroy;
        }

        #region IConvertContract<TContract,TTarget> Implementation

        public TTarget Create(TContract contract)
        {
            return _create(contract);
        }

        public void Destroy(TTarget target)
        {
            _destroy(target);
        }

        #endregion
    }
}