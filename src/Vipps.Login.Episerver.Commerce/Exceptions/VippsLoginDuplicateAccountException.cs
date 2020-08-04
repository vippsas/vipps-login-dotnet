using System;

namespace Vipps.Login.Episerver.Commerce.Exceptions
{
    public class VippsLoginDuplicateAccountException : Exception
    {
        public VippsLoginDuplicateAccountException(string message)
              : base(message)
        {
        }
    }
}
