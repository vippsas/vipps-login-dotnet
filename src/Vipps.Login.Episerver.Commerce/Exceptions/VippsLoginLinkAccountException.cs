using System;

namespace Vipps.Login.Episerver.Commerce.Exceptions
{
    public class VippsLoginLinkAccountException : Exception
    {
        public VippsLoginLinkAccountException(string message, bool userError = false)
            : base(message)
        {
            UserError = userError;
        }

        public bool UserError { get; set; }
    }
}