namespace Fp.DataBinding.Binds
{
    public class FailedSyncCollectionException : BindContextException
    {
        public FailedSyncCollectionException() { }

        public FailedSyncCollectionException(string msg) : base(msg) { }
    }
}