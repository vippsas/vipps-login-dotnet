using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;
using Vipps.Login.Episerver.Commerce.Extensions;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginCommerceService : IVippsLoginCommerceService
    {
        private readonly IVippsLoginService _vippsLoginService;

        public VippsLoginCommerceService(
            IVippsLoginService vippsLoginService)
        {
            _vippsLoginService = vippsLoginService;
        }

        public IEnumerable<CustomerContact> FindCustomerContacts(Guid subjectGuid)
        {
            return BusinessManager
                .List(ContactEntity.ClassName, new[]
                {
                    new FilterElement(
                        MetadataConstants.VippsSubjectGuidFieldName,
                        FilterElementType.Equal,
                        subjectGuid
                    )
                })
                .OfType<CustomerContact>();
        }

        public IEnumerable<CustomerContact> FindCustomerContacts(string email, string phone)
        {
            //TODO: search on phone number
            return BusinessManager
                .List(ContactEntity.ClassName, new[]
                {
                    new FilterElement(
                        "Email",
                        FilterElementType.Equal,
                        email
                    )
                })
                .OfType<CustomerContact>();
        }

        public void SyncInfo(IIdentity identity, CustomerContact currentContact, VippsSyncOptions options = default)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (currentContact == null)
                throw new ArgumentNullException(nameof(currentContact));
            if (options == null)
                options = new VippsSyncOptions();

            var vippsUserInfo = _vippsLoginService.GetVippsUserInfo(identity);
            if (vippsUserInfo == null)
            {
                return;
            }

            // Always sync vipps subject guid
            currentContact.SetVippsSubject(vippsUserInfo.Sub);
            if (options.SyncContactInfo)
            {
                // Maps fields onto customer contact
                // Vipps email, firstname, lastname, fullname, birthdate
                MapVippsContactFields(currentContact, vippsUserInfo);
            }

            if (options.SyncAddresses)
            {
                SyncAddresses(identity, currentContact, options.AddressType);
            }

            currentContact.SaveChanges();
        }

        protected virtual void SyncAddresses(
            IIdentity identity,
            CustomerContact currentContact,
            CustomerAddressTypeEnum addressType)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (currentContact == null)
                throw new ArgumentNullException(nameof(currentContact));

            var vippsUserInfo = _vippsLoginService.GetVippsUserInfo(identity);
            if (vippsUserInfo == null)
            {
                return;
            }

            foreach (var vippsAddress in vippsUserInfo.Addresses)
            {
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
                address.DaytimePhoneNumber = address.EveningPhoneNumber = vippsUserInfo.PhoneNumber;

                if (isNewAddress)
                {
                    currentContact.AddContactAddress(address);
                }
                else
                {
                    currentContact.UpdateContactAddress(address);
                }
            }
        }

        protected virtual void MapVippsContactFields(
            CustomerContact contact,
            VippsUserInfo userInfo
        )
        {
            if (userInfo == null) throw new ArgumentNullException(nameof(userInfo));

            contact.Email = userInfo.Email;
            contact.FirstName = userInfo.GivenName;
            contact.LastName = userInfo.FamilyName;
            contact.FullName = userInfo.Name;
            contact.BirthDate = userInfo.BirthDate;
        }

        protected virtual void MapVippsAddressFields(
            CustomerAddress address,
            VippsAddress vippsAddress
        )
        {
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