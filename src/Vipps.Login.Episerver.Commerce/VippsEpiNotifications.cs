using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Mediachase.Commerce.Customers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsEpiNotifications : VippsOpenIdConnectAuthenticationNotifications
    {
#pragma warning disable 649
        private Injected<IVippsLoginService> _vippsLoginService;
        private Injected<VippsLoginCommerceService> _vippsCommerceService;
        private Injected<MapUserKey> _mapUserKey;
#pragma warning restore 649

        public VippsEpiNotifications()
        {
            SecurityTokenValidated = DefaultSecurityTokenValidated;
        }

        private async Task DefaultSecurityTokenValidated(
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
            var vippsInfo = _vippsLoginService.Service.GetVippsUserInfo(identity);

            // First check if we already have a contact for this sub
            var emailAddress = FindBySubjectGuid(vippsInfo?.Sub);
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                // Try to find contact by vipps email/phone number
                var contacts = FindByVippsInfo(vippsInfo).ToArray();
                if (contacts.Length == 1)
                {
                    // TODO: implement 'Sanity check'
                    emailAddress = GetLoginEmailFromContact(contacts.First());
                }
                else if (contacts.Length > 1)
                {
                    throw new VippsLoginDuplicateAccountException(
                        "Multiple accounts found matching your Vipps info. Please log in and link your account through your profile page."
                    );
                }
            }

            // New user
            if (string.IsNullOrWhiteSpace(emailAddress))
            {
                emailAddress = identity.FindFirst(ClaimTypes.Email)?.Value;
            }

            // By default we use email address as username
            if (!identity.HasClaim(x => x.Type.Equals(identity.NameClaimType)))
            {
                identity.AddClaim(
                    new Claim(identity.NameClaimType, emailAddress)
                );
            }

            // Sync user and the roles to Epi
            await ServiceLocator.Current.GetInstance<ISynchronizingUserService>()
                .SynchronizeAsync(identity, new List<string>());
        }

        private IEnumerable<CustomerContact> FindByVippsInfo(VippsUserInfo vippsInfo)
        {
            return _vippsCommerceService.Service.FindCustomerContacts(vippsInfo.Email, vippsInfo.PhoneNumber);
        }

        protected virtual string FindBySubjectGuid(Guid? subjectGuid)
        {
            if (!subjectGuid.HasValue)
            {
                return null;
            }

            var customerContact = _vippsCommerceService.Service
                .FindCustomerContact(subjectGuid.Value);
            return customerContact == null ? null : GetLoginEmailFromContact(customerContact);
        }

        private string GetLoginEmailFromContact(CustomerContact customerContact)
        {
            return _mapUserKey.Service.ToUserKey(customerContact?.UserId)?.ToString();
        }
    }
}