using Mediachase.Commerce.Customers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using EPiServer.Logging;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Vipps.Login.Episerver.Commerce.Extensions;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginCommerceService : IVippsLoginCommerceService
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(VippsLoginCommerceService));
        private readonly IVippsLoginService _vippsLoginService;
        private readonly IVippsLoginMapper _vippsLoginMapper;
        private readonly IVippsLoginDataLoader _vippsLoginDataLoader;
        private readonly ICustomerContactService _customerContactService;

        public VippsLoginCommerceService(
            IVippsLoginService vippsLoginService,
            IVippsLoginMapper vippsLoginMapper,
            IVippsLoginDataLoader vippsLoginDataLoader,
            ICustomerContactService customerContactService)
        {
            _vippsLoginService = vippsLoginService;
            _vippsLoginMapper = vippsLoginMapper;
            _vippsLoginDataLoader = vippsLoginDataLoader;
            _customerContactService = customerContactService;
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
                _customerContactService.SaveChanges(currentContact);
            }
        }

        public void RemoveLinkToVippsAccount(CustomerContact contact)
        {
            if (contact == null) throw new ArgumentNullException(nameof(contact));
            contact.SetVippsSubject(null);
            _customerContactService.SaveChanges(contact);
        }

        public CustomerContact FindCustomerContactByLinkAccountToken(Guid linkAccountToken)
        {
            var contacts = _vippsLoginDataLoader.FindContactsByLinkAccountToken(linkAccountToken).ToList();
            if (contacts.Count() > 1)
            {
                Logger.Warning(
                    $"Vipps.Login: found more than one account for subjectGuid {linkAccountToken}. Fallback to use first result.");
            }

            return contacts.FirstOrDefault();
        }

        public bool HandleLogin(
            IOwinContext context,
            VippsSyncOptions vippsSyncOptions = default,
            CustomerContact customerContact = null)
        {
            var isAuthenticated = context.Authentication.User?.Identity?.IsAuthenticated ?? false;
            if (!isAuthenticated)
            {
                // Regular log in
                context.Authentication.Challenge(VippsAuthenticationDefaults.AuthenticationType);
                return true;
            }

            // Make sure to sync vipps info (required for at least the identifier)
            // You can use the VippsSyncOptions to determine what else to sync (contact/address info)
            SyncInfo(
                context.Authentication.User.Identity,
                customerContact ?? CustomerContext.Current.CurrentContact,
                vippsSyncOptions);

            return false;
        }

        public bool HandleLinkAccount(
            IOwinContext context,
            CustomerContact customerContact = null)
        {
            var isAuthenticated = context.Authentication.User?.Identity?.IsAuthenticated ?? false;
            if (!isAuthenticated)
            {
                throw new InvalidOperationException();
            }

            var isVippsIdentity = _vippsLoginService
                .IsVippsIdentity(context.Authentication.User.Identity);
            if (isVippsIdentity)
            {
                return false;
            }

            // Link Vipps account to current logged in user account
            context.Authentication.Challenge(
                new AuthenticationProperties(new Dictionary<string, string>
                {
                    {
                        VippsConstants.LinkAccount,
                        CreateLinkAccountToken(customerContact ?? CustomerContext.Current.CurrentContact)
                            .ToString()
                    }
                }), VippsAuthenticationDefaults.AuthenticationType);
            return true;
        }

        public bool HandleRedirect(IOwinContext context, string returnUrl)
        {
            // Prevent redirecting to external Uris
            var redirectUri = new Uri(returnUrl, UriKind.RelativeOrAbsolute);
            if (redirectUri.IsAbsoluteUri)
            {
                returnUrl = redirectUri.PathAndQuery;
            }

            context.Response.Redirect(returnUrl);
            return true;
        }

        public Guid CreateLinkAccountToken(CustomerContact contact)
        {
            var token = Guid.NewGuid();
            contact.SetVippsLinkAccountToken(token);
            _customerContactService.SaveChanges(contact);
            return token;
        }
    }
}