using System;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginDuplicateAccountException : Exception
    {
        public VippsLoginDuplicateAccountException(string message)
              : base(message)
        {
        }
    }
}
