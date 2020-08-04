using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using EPiServer.Logging;
using Vipps.Login.Episerver.Commerce.Extensions;
using Vipps.Login.Models;

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
                Logger.Warning(
                    $"Vipps.Login: found more than one account for subjectGuid {subjectGuid}. Fallback to use first result.");
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

            if (options.SyncAddresses && vippsUserInfo.Addresses != null)
            {
                foreach (var vippsAddress in vippsUserInfo.Addresses)
                {
                    _vippsLoginMapper.MapAddress(
                        currentContact,
                        options.AddressType,
                        vippsAddress,
                        vippsUserInfo.PhoneNumber);
                }
            }

            if (options.ShouldSaveContact)
            {
                currentContact.SaveChanges();
            }
        }
    }
}