using System;

namespace Fp.DataBinding
{
    public class BindContextException : Exception
    {
        public BindContextException() { }

        public BindContextException(string msg) : base(msg) { }
    }
}