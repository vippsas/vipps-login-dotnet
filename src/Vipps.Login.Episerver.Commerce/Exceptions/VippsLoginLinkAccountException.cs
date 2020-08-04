using System;

namespace Vipps.Login.Episerver.Commerce.Exceptions
{
    public class VippsLoginLinkAccountException : Exception
    {
        public VippsLoginLinkAccountException(string message)
            : base(message)
        {
        }
    }
}