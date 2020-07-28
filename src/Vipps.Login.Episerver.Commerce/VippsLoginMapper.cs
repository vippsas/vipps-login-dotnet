using System;
using Mediachase.Commerce.Customers;
using Vipps.Login.Episerver.Commerce.Extensions;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginMapper : IVippsLoginMapper
    {

        public virtual void MapVippsContactFields(
            CustomerContact contact,
            VippsUserInfo userInfo
        )
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            if (userInfo == null) throw new ArgumentNullException(nameof(userInfo));

            contact.Email = userInfo.Email;
            contact.FirstName = userInfo.GivenName;
            contact.LastName = userInfo.FamilyName;
            contact.FullName = userInfo.Name;
            contact.BirthDate = userInfo.BirthDate;
        }

        public virtual void MapVippsAddressFields(
            CustomerAddress address,
            VippsAddress vippsAddress
        )
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (vippsAddress == null) throw new ArgumentNullException(nameof(vippsAddress));

            address.Name = $"Vipps - {address.GetVippsAddressType()}";
            address.Line1 = vippsAddress.StreetAddress;
            address.City = vippsAddress.Region;
            address.PostalCode = vippsAddress.PostalCode;
            //TODO: map country code
            address.CountryCode = vippsAddress.Country;
            address.SetVippsAddressType(vippsAddress.AddressType);
        }
    }
}