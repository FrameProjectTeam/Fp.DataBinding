using System;

namespace Fp.DataBinding.Binds.Collections
{
    public interface ICollectionBind : IDisposable
    {
        bool IsWriting { get; }
        bool IsReading { get; }
        bool IsActive { get; }
        int ActiveDepth { get; }
        bool IsLastInitiator { get; }
        bool IsLastInteractor { get; }
    }
}