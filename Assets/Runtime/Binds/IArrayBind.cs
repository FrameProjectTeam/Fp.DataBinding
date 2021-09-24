using System;

using UniRx;

namespace Fp.DataBinding.Binds
{
    public interface IArrayBind<T> : IReadOnlyReactiveCollection<T>, IDisposable { }
}