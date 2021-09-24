namespace Fp.DataBinding.Binds
{
    public sealed class DataContextConverter<TContract> : IConvertContract<TContract, BindContext>
        where TContract : IDataContext
    {
        private readonly BindContext _mainBindContext;

        public DataContextConverter(BindContext mainBindContext)
        {
            _mainBindContext = mainBindContext;
        }

        #region IConvertContract<TContract,BindContext> Implementation

        public BindContext Create(TContract contract)
        {
            BindContext subCtx = _mainBindContext.SubContext();
            contract.Bind(subCtx);
            return subCtx;
        }

        public void Destroy(BindContext target)
        {
            _mainBindContext.ReturnContext(target);
        }

        #endregion
    }
}