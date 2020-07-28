using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using EPiServer.Logging;
using Vipps.Login.Episerver.Commerce.Extensions;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginCommerceService : IVippsLoginCommerceService
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(VippsLoginCommerceService));
        private readonly IVippsLoginService _vippsLoginService;
        private readonly IVippsLoginMapper _vippsLoginMapper;
        private readonly IVippsLoginDataLoader _vippsLoginDataLoader;

        public VippsLoginCommerceService(
            IVippsLoginService vippsLoginService,
            IVippsLoginMapper vippsLoginMapper,
            IVippsLoginDataLoader vippsLoginDataLoader)
        {
            _vippsLoginService = vippsLoginService;
            _vippsLoginMapper = vippsLoginMapper;
            _vippsLoginDataLoader = vippsLoginDataLoader;
        }

        public CustomerContact FindCustomerContact(Guid subjectGuid)
        {
            var contacts = _vippsLoginDataLoader.FindContactsBySubjectGuid(subjectGuid).ToList();
            if (contacts.Count() > 1)
            {
                Logger.Warning($"Vipps.Login: found more than one account for subjectGuid {subjectGuid}. Fallback to use first result.");
            }
            return contacts.FirstOrDefault();
        }

        public IEnumerable<CustomerContact> FindCustomerContacts(string email, string phone)
        {
            var byEmail = _vippsLoginDataLoader.FindContactsByEmail(email);
            var byPhone = _vippsLoginDataLoader.FindContactsByPhone(phone);

            // return distinct list
            return byEmail
                .Union(byPhone)
                .Where(x => x.PrimaryKeyId.HasValue)
                .GroupBy(x => x.PrimaryKeyId)
                .Select(x => x.First());
        }

        public void SyncInfo(IIdentity identity, CustomerContact currentContact, VippsSyncOptions options = default)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (currentContact == null)
            {
                throw new ArgumentNullException(nameof(currentContact));
            }

            if (options == null)
            {
                options = new VippsSyncOptions();
            }

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
                _vippsLoginMapper.MapVippsContactFields(currentContact, vippsUserInfo);
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
            {
                throw new ArgumentNullException(nameof(identity));
            }

            if (currentContact == null)
            {
                throw new ArgumentNullException(nameof(currentContact));
            }

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
                _vippsLoginMapper.MapVippsAddressFields(address, vippsAddress);
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
    }
}