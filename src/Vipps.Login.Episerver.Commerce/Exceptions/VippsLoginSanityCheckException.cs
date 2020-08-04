using System;

namespace Vipps.Login.Episerver.Commerce.Exceptions
{
    public class VippsLoginSanityCheckException : Exception
    {
        public VippsLoginSanityCheckException(string message)
            : base(message)
        {
        }
    }
}