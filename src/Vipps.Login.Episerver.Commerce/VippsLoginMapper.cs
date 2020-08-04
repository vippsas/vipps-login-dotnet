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
            if (!string.IsNullOrWhiteSpace(userInfo.GivenName))
            {
                contact.FirstName = userInfo.GivenName;
            }
            if (!string.IsNullOrWhiteSpace(userInfo.FamilyName))
            {
                contact.LastName = userInfo.FamilyName;
            }
            if (!string.IsNullOrWhiteSpace(userInfo.Name))
            {
                contact.FullName = userInfo.Name;
            }
            if (userInfo.BirthDate.HasValue)
            {
                contact.BirthDate = userInfo.BirthDate;
            }
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
            address.CountryCode = ToEpiCountryCode(vippsAddress.Country);
            address.SetVippsAddressType(vippsAddress.AddressType);
        }

        public virtual void MapAddress(
            CustomerContact currentContact,
            CustomerAddressTypeEnum addressType,
            VippsAddress vippsAddress,
            string phoneNumber)
        {
            if (currentContact == null) throw new ArgumentNullException(nameof(currentContact));
            if (vippsAddress == null) throw new ArgumentNullException(nameof(vippsAddress));
            // Vipps addresses don't have an ID
            // They can be identified by Vipps address type
            var address =
                currentContact.ContactAddresses.FindVippsAddress(vippsAddress.AddressType);
            var isNewAddress = address == null;
            if (isNewAddress)
            {
                address = CustomerAddress.CreateInstance();
                address.AddressType = addressType;
            }

            // Maps fields onto customer address:
            // Vipps address type, street, city, postalcode, countrycode
            MapVippsAddressFields(address, vippsAddress);
            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                address.DaytimePhoneNumber = address.EveningPhoneNumber = phoneNumber;
            }

            if (isNewAddress)
            {
                currentContact.AddContactAddress(address);
            }
            else
            {
                currentContact.UpdateContactAddress(address);
            }
        }

        protected virtual string ToEpiCountryCode(string vippsCountryCode)
        {
            // Map country code to epi country code (three letter)
            // As only Norwegian addresses are supported, decided to keep it simple
            if (vippsCountryCode.Equals("no", StringComparison.InvariantCultureIgnoreCase))
            {
                return "NOR";
            }

            return vippsCountryCode;
        }
    }
}