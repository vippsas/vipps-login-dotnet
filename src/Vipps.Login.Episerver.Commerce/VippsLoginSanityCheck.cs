using System;
using Mediachase.Commerce.Customers;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginSanityCheck : IVippsLoginSanityCheck
    {
        public bool IsValidContact(CustomerContact contact, VippsUserInfo userInfo)
        {
            if (contact == null || userInfo == null)
            {
                return false;
            }

            return
                (userInfo.GivenName ?? string.Empty).Equals(contact.FirstName, StringComparison.InvariantCultureIgnoreCase) &&
                (userInfo.FamilyName ?? string.Empty).Equals(contact.LastName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}