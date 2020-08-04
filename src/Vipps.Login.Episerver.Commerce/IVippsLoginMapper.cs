using Mediachase.Commerce.Customers;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public interface IVippsLoginMapper
    {
        void MapVippsContactFields(
            CustomerContact contact,
            VippsUserInfo userInfo
        );

        void MapVippsAddressFields(
            CustomerAddress address,
            VippsAddress vippsAddress
        );

        void MapAddress(
            CustomerContact currentContact,
            CustomerAddressTypeEnum addressType,
            VippsAddress vippsAddress,
            string phoneNumber
        );
    }
}