using Mediachase.Commerce.Customers;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public interface IVippsLoginSanityCheck
    {
        bool IsValidContact(CustomerContact contact, VippsUserInfo userInfo);
    }
}