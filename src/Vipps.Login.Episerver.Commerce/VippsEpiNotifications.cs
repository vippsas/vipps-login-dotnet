using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Vipps.Login.Episerver.Commerce.Exceptions;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsEpiNotifications : VippsOpenIdConnectAuthenticationNotifications
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(VippsEpiNotifications));

#pragma warning disable 649
        private Injected<ISynchronizingUserService> _synchronizingUserServiceInjected;
        private Injected<IVippsLoginService> _vippsLoginServiceInjected;
        private Injected<IVippsLoginCommerceService> _vippsCommerceServiceInjected;
        private Injected<IVippsLoginSanityCheck> _vippsLoginSanityCheckInjected;
        private Injected<MapUserKey> _mapUserKeyInjected;
#pragma warning restore 649

        private readonly ISynchronizingUserService _synchronizingUserService;
        private readonly IVippsLoginService _vippsLoginService;
        private readonly IVippsLoginCommerceService _vippsCommerceService;
        private readonly IVippsLoginSanityCheck _vippsLoginSanityCheck;
        private readonly MapUserKey _mapUserKey;

        public VippsEpiNotifications()
        {
            SecurityTokenValidated = DefaultSecurityTokenValidated;
            _synchronizingUserService = _synchronizingUserServiceInjected.Service;
            _vippsLoginService = _vippsLoginServiceInjected.Service;
            _vippsCommerceService = _vippsCommerceServiceInjected.Service;
            _vippsLoginSanityCheck = _vippsLoginSanityCheckInjected.Service;
            _mapUserKey = _mapUserKeyInjected.Service;
        }

        public VippsEpiNotifications(
            ISynchronizingUserService synchronizingUserService,
            IVippsLoginService vippsLoginService,
            IVippsLoginCommerceService vippsLoginCommerceService,
            IVippsLoginSanityCheck vippsLoginSanityCheck,
            MapUserKey mapUserKey
        ) : base()
        {
            SecurityTokenValidated = DefaultSecurityTokenValidated;
            _synchronizingUserService = synchronizingUserService;
            _vippsLoginService = vippsLoginService;
            _vippsCommerceService = vippsLoginCommerceService;
            _vippsLoginSanityCheck = vippsLoginSanityCheck;
            _mapUserKey = mapUserKey;
        }

        public VippsEpiNotifications(
            HttpClient httpClient,
            ISynchronizingUserService synchronizingUserService,
            IVippsLoginService vippsLoginService,
            IVippsLoginCommerceService vippsLoginCommerceService,
            IVippsLoginSanityCheck vippsLoginSanityCheck,
            MapUserKey mapUserKey
        ) : base(httpClient)
        {
            SecurityTokenValidated = DefaultSecurityTokenValidated;
            _synchronizingUserService = synchronizingUserService;
            _vippsLoginService = vippsLoginService;
            _vippsCommerceService = vippsLoginCommerceService;
            _vippsLoginSanityCheck = vippsLoginSanityCheck;
            _mapUserKey = mapUserKey;
        }

        protected override void AvoidRedirectLoop(
            RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            var properties = context.OwinContext.Authentication.AuthenticationResponseChallenge.Properties.Dictionary;
            
            // Allow LinkAccount request to pass through
            if (properties.ContainsKey(VippsConstants.LinkAccount) &&
                !_vippsLoginService.IsVippsIdentity(context.OwinContext.Authentication.User?.Identity))
            {
                return;
            }

            base.AvoidRedirectLoop(context);
        }

        public async Task DefaultSecurityTokenValidated(
            SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context
        )
        {
            // Prevent redirecting to external Uris
            var redirectUri = new Uri(context.AuthenticationTicket.Properties.RedirectUri,
                UriKind.RelativeOrAbsolute);
            if (redirectUri.IsAbsoluteUri)
            {
                context.AuthenticationTicket.Properties.RedirectUri = redirectUri.PathAndQuery;
            }

            var identity = context.AuthenticationTicket.Identity;
            var vippsInfo = _vippsLoginService.GetVippsUserInfo(identity);
            if (vippsInfo == null)
            {
                var message = "Could not retrieve Vipps UserInfo from the provided identity";
                Logger.Error($"Vipps.Login: {message}");
                throw new VippsLoginException(message);
            }

            // First check if we already have a contact for this sub
            var emailAddress = FindBySubjectGuid(vippsInfo.Sub);
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                // Try to find contact by vipps email/phone number
                var contacts = FindByVippsInfo(vippsInfo).ToArray();
                if (contacts.Length == 1)
                {
                    var contact = contacts.First();
                    if (!_vippsLoginSanityCheck.IsValidContact(contact, vippsInfo))
                    {
                        var message = "Existing contact does not pass verification.";
                        Logger.Warning($"Vipps.Login: {message}. Subject Guid: {vippsInfo.Sub}");
                        throw new VippsLoginSanityCheckException(message);
                    }
                    emailAddress = GetLoginEmailFromContact(contact);
                }
                else if (contacts.Length > 1)
                {
                    var message = "Multiple accounts found matching this Vipps UserInfo";
                    Logger.Warning($"Vipps.Login: {message}. Subject Guid: {vippsInfo.Sub}");
                    throw new VippsLoginDuplicateAccountException(message);
                }
            }

            // New user
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                emailAddress = vippsInfo.Email;
            }

            // By default Epi will use email address as username
            if (!identity.HasClaim(x => x.Type.Equals(identity.NameClaimType)))
            {
                identity.AddClaim(
                    new Claim(identity.NameClaimType, emailAddress)
                );
            }

            // Sync user and the roles to Epi
            await _synchronizingUserService
                .SynchronizeAsync(identity, new List<string>());
        }

        private IEnumerable<CustomerContact> FindByVippsInfo(VippsUserInfo vippsInfo)
        {
            return _vippsCommerceService.FindCustomerContacts(vippsInfo.Email, vippsInfo.PhoneNumber);
        }

        protected virtual string FindBySubjectGuid(Guid? subjectGuid)
        {
            if (!subjectGuid.HasValue)
            {
                return null;
            }

            var customerContact = _vippsCommerceService
                .FindCustomerContact(subjectGuid.Value);
            return customerContact == null ? null : GetLoginEmailFromContact(customerContact);
        }

        private string GetLoginEmailFromContact(CustomerContact customerContact)
        {
            return _mapUserKey.ToUserKey(customerContact?.UserId)?.ToString();
        }
    }
}