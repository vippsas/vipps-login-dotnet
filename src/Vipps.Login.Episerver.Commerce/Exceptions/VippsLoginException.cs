using System;

namespace Vipps.Login.Episerver.Commerce.Exceptions
{
    public class VippsLoginException : Exception
    {
        public VippsLoginException(string message)
            : base(message)
        {
        }
    }
}