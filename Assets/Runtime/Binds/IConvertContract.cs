namespace Fp.DataBinding.Binds
{
    public interface IConvertContract<in TContract, TTarget>
    {
        TTarget Create(TContract contract);
        void Destroy(TTarget target);
    }
}