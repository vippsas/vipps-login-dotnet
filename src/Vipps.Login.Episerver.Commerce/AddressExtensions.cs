using System.Collections.Generic;
using System.Linq;
using Mediachase.Commerce.Customers;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce.Episerver
{
    public static class AddressExtensions
    {
        public static CustomerAddress FindVippsAddress(this IEnumerable<CustomerAddress> addresses, VippsAddress address)
        {
           return addresses.FirstOrDefault(x =>
               x.GetVippsAddressType().Equals(address.AddressType));
        }

        public static CustomerAddress FindAllVippsAddresses(this IEnumerable<CustomerAddress> addresses)
        {
            return addresses.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.GetVippsAddressType()));
        }

        public static void SetVippsAddressType(this CustomerAddress address, string addressType)
        {
            address[MetadataConstants.VippsAddressTypeFieldName] = addressType;
        }

        public static string GetVippsAddressType(this CustomerAddress address)
        {
            return address[MetadataConstants.VippsAddressTypeFieldName]?.ToString() ?? string.Empty;
        }

        public static void MapAddress(
            this CustomerAddress address,
            VippsAddress vippsAddress
        )
        {
            address.Line1 = vippsAddress.StreetAddress;
            address.City = vippsAddress.Region;
            address.PostalCode = vippsAddress.PostalCode;
            address.CountryCode = vippsAddress.Country;
            address.SetVippsAddressType(vippsAddress.AddressType);
        }
    }
}