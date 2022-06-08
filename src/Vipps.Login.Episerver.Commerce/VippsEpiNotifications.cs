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
            SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            // Prevent redirecting to external Uris
            var redirectUri = new Uri(context.AuthenticationTicket.Properties.RedirectUri,
                UriKind.RelativeOrAbsolute);
            if (redirectUri.IsAbsoluteUri)
            {
                context.AuthenticationTicket.Properties.RedirectUri = redirectUri.PathAndQuery;
            }

            var identity = await GetClaimsIdentity(context).ConfigureAwait(false);

            var userId = GetEpiUserId(context, identity);
            if (string.IsNullOrWhiteSpace(userId))
            {
                var message = "Could not get UserId for the provided Vipps identity";
                Logger.Error($"Vipps.Login: {message}");
                throw new VippsLoginException(message);
            }

            // Set identifier on ClaimsIdentity
            // Important, Name is also used to find CustomerContact
            if (!identity.HasClaim(x => x.Type.Equals(identity.NameClaimType)))
            {
                identity.AddClaim(
                    new Claim(identity.NameClaimType, userId)
                );
            }

            // Sync ClaimsIdentity and the roles to Epi db
            await _synchronizingUserService
                .SynchronizeAsync(identity, new List<string>());
        }

        protected virtual async Task<ClaimsIdentity> GetClaimsIdentity(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            var identity = context.AuthenticationTicket.Identity;
            
            var configuration =
                await context.Options.ConfigurationManager
                    .GetConfigurationAsync(context.Request.CallCancelled)
                    .ConfigureAwait(false);

            // Use access token to retrieve claims from UserInfo endpoint
            var userInfoClaims = await _vippsLoginService.GetUserInfoClaims(
                configuration.UserInfoEndpoint,
                context.ProtocolMessage.AccessToken)
                .ConfigureAwait(false);
            // Add claims to identity
            identity.AddClaims(userInfoClaims);
            
            return identity;
        }

        protected virtual string GetEpiUserId(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context, ClaimsIdentity identity)
        {
            var vippsInfo = _vippsLoginService.GetVippsUserInfo(identity);
            if (vippsInfo == null)
            {
                var message = "Could not retrieve Vipps UserInfo from the provided identity";
                Logger.Error($"Vipps.Login: {message}");
                throw new VippsLoginException(message);
            }

            // Get existing user identifier (by default Epi will use email address)
            var userId =
                // First check if we're linking to an existing account
                ByLinkAccount(context, vippsInfo) ??
                // If not, find user by vipps identifier (subject guid)
                BySubjectGuid(vippsInfo) ??
                // Or else search user by email/phone
                ByEmailOrPhoneNumber(vippsInfo);

            // New user
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = vippsInfo.Email;
            }

            return userId;
        }

        // Check if we're trying to link an existing account
        protected virtual string ByLinkAccount(
            SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context,
            VippsUserInfo vippsUserInfo)
        {
            var props = context.AuthenticationTicket.Properties.Dictionary;
            if (props.ContainsKey(VippsConstants.LinkAccount) &&
                Guid.TryParse(props[VippsConstants.LinkAccount], out var linkAccountToken))
            {
                // Do not allow linking to multiple accounts
                string message;
                if (BySubjectGuid(vippsUserInfo) != null)
                {
                    message =
                        "This Vipps account is already linked to an account. Please remove the connection before making a new one.";
                    Logger.Error($"Vipps.Login: {message}");
                    throw new VippsLoginLinkAccountException(message, true);
                }

                var accountToLink = _vippsCommerceService.FindCustomerContactByLinkAccountToken(linkAccountToken);
                if (accountToLink == null)
                {
                    message = "Could not find account to link to.";
                    Logger.Error($"Vipps.Login: {message}");
                    throw new VippsLoginLinkAccountException(message);
                }

                return GetLoginEmailFromContact(accountToLink);
            }

            return null;
        }

        // Check if we already have a contact for this sub
        protected virtual string BySubjectGuid(VippsUserInfo vippsInfo)
        {
            if (vippsInfo?.Sub == null)
            {
                return null;
            }

            var customerContact = _vippsCommerceService
                .FindCustomerContact(vippsInfo.Sub);
            return customerContact == null ? null : GetLoginEmailFromContact(customerContact);
        }

        // Find contact by vipps email/phone number
        protected virtual string ByEmailOrPhoneNumber(VippsUserInfo vippsInfo)
        {
            var contacts = _vippsCommerceService
                .FindCustomerContacts(vippsInfo.Email, vippsInfo.PhoneNumber)
                .ToArray();
            if (contacts.Length == 1)
            {
                var contact = contacts.First();
                if (!_vippsLoginSanityCheck.IsValidContact(contact, vippsInfo))
                {
                    var message = "Existing contact does not pass verification.";
                    Logger.Warning($"Vipps.Login: {message}. Subject Guid: {vippsInfo.Sub}");
                    throw new VippsLoginSanityCheckException(message);
                }

                return GetLoginEmailFromContact(contact);
            }

            if (contacts.Length > 1)
            {
                var message = "Multiple accounts found matching this Vipps UserInfo";
                Logger.Warning($"Vipps.Login: {message}. Subject Guid: {vippsInfo.Sub}");
                throw new VippsLoginDuplicateAccountException(message);
            }

            return null;
        }

        private string GetLoginEmailFromContact(CustomerContact customerContact)
        {
            return _mapUserKey.ToUserKey(customerContact?.UserId)?.ToString();
        }
    }
}