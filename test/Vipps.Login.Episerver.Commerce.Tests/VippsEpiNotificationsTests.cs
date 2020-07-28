using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EPiServer.Security;
using FakeItEasy;
using Mediachase.Commerce.Customers;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Vipps.Login.Episerver.Commerce.Exceptions;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class VippsEpiNotificationsTests
    {
        [Fact]
        public async Task DefaultSecurityTokenValidatedThrowsIfUserInfoIsNull()
        {
            var vippsLoginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => vippsLoginService.GetVippsUserInfo(A<ClaimsIdentity>._)).Returns(null);
            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                vippsLoginService,
                A.Fake<IVippsLoginCommerceService>(),
                A.Fake<IVippsLoginSanityCheck>(),
                A.Fake<MapUserKey>()
            );
            
            await Assert.ThrowsAsync<VippsLoginException>(async () =>
                await notifications.DefaultSecurityTokenValidated(CreateContext()));
        }

        [Fact]
        public async Task DefaultSecurityTokenValidatedRewritesRedirectUriToLocal()
        {
            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginCommerceService>(),
                A.Fake<IVippsLoginSanityCheck>(),
                A.Fake<MapUserKey>()
            );

            var context = CreateContext();
            await notifications.DefaultSecurityTokenValidated(context);

            Assert.Equal("/redirect-url", context.AuthenticationTicket.Properties.RedirectUri);
        }

        // New user
        // No contact with subject guid
        // No contact with phone or by email
        [Fact]
        public async Task DefaultSecurityTokenValidatedSetsVippsEmailAsNameClaim()
        {
            var testEmail = "test@test.com";
            var testPhoneNumber = "0612345678";

            var vippsLoginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => vippsLoginService.GetVippsUserInfo(A<ClaimsIdentity>._))
                .Returns(new VippsUserInfo
                {
                    Email = testEmail,
                    PhoneNumber = testPhoneNumber
                });
            
            // No contact with subject guid
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(null);

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                vippsLoginService,
                vippsCommerceService,
                A.Fake<IVippsLoginSanityCheck>(),
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();

            await notifications.DefaultSecurityTokenValidated(context);
            Assert.True(
                context.AuthenticationTicket.Identity.HasClaim(
                    context.AuthenticationTicket.Identity.NameClaimType,
                    testEmail)
            );
        }

        // Existing user
        // Find contact with matching subject guid
        [Fact]
        public async Task DefaultSecurityTokenValidatedSetsEmailAsNameClaim()
        {
            var testEmail = "test@test.com";
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(new CustomerContact() {UserId = testEmail});
           
            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                vippsCommerceService,
                A.Fake<IVippsLoginSanityCheck>(),
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();

            await notifications.DefaultSecurityTokenValidated(context);
            Assert.True(
                context.AuthenticationTicket.Identity.HasClaim(
                    context.AuthenticationTicket.Identity.NameClaimType,
                    testEmail)
            );
        }

        // Existing user
        // No contact with subject guid
        // Find with phone or by email
        [Fact]
        public async Task DefaultSecurityTokenValidatedSetsFirstCustomerAsNameClaim()
        {
            var testEmail = "test@test.com";
            var testPhoneNumber = "0612345678";

            var vippsLoginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => vippsLoginService.GetVippsUserInfo(A<ClaimsIdentity>._))
                .Returns(new VippsUserInfo
                {
                    Email = testEmail,
                    PhoneNumber = testPhoneNumber
                });

            // No contact with subject guid
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(null);

            // Find with phone or by email
            A.CallTo(() => vippsCommerceService.FindCustomerContacts(testEmail, testPhoneNumber))
                .Returns(new [] { new CustomerContact { UserId = testEmail } });

            var sanityCheck = A.Fake<IVippsLoginSanityCheck>();
            A.CallTo(() => sanityCheck.IsValidContact(A<CustomerContact>._, A<VippsUserInfo>._))
                .Returns(true);

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                vippsLoginService,
                vippsCommerceService,
                sanityCheck,
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();

            await notifications.DefaultSecurityTokenValidated(context);
            Assert.True(
                context.AuthenticationTicket.Identity.HasClaim(
                    context.AuthenticationTicket.Identity.NameClaimType,
                    testEmail)
            );
        }

        // Existing user
        // No contact with subject guid
        // Find with phone or by email
        // Fail sanity check
        [Fact]
        public async Task DefaultSecurityTokenValidatedThrowsIfSanityCheckFails()
        {
            var testEmail = "test@test.com";
            var testPhoneNumber = "0612345678";

            var vippsLoginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => vippsLoginService.GetVippsUserInfo(A<ClaimsIdentity>._))
                .Returns(new VippsUserInfo
                {
                    Email = testEmail,
                    PhoneNumber = testPhoneNumber
                });

            // No contact with subject guid
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(null);

            // Find with phone or by email
            A.CallTo(() => vippsCommerceService.FindCustomerContacts(testEmail, testPhoneNumber))
                .Returns(new[] { new CustomerContact { UserId = testEmail } });

            // Failing sanity check
            var sanityCheck = A.Fake<IVippsLoginSanityCheck>();
            A.CallTo(() => sanityCheck.IsValidContact(A<CustomerContact>._, A<VippsUserInfo>._))
                .Returns(false);

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                vippsLoginService,
                vippsCommerceService,
                sanityCheck,
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();

            await Assert.ThrowsAsync<VippsLoginSanityCheckException>(async () =>
                await notifications.DefaultSecurityTokenValidated(context));
        }

        // Existing users
        // No contact with subject guid
        // Find multiple with phone or by email
        [Fact]
        public async Task DefaultSecurityTokenValidatedThrowsIfMultipleContactsFound()
        {
            var testEmail = "test@test.com";
            var testPhoneNumber = "0612345678";

            var vippsLoginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => vippsLoginService.GetVippsUserInfo(A<ClaimsIdentity>._))
                .Returns(new VippsUserInfo
                {
                    Email = testEmail,
                    PhoneNumber = testPhoneNumber
                });

            // No contact with subject guid
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(null);

            // Find multiple with phone or by email
            A.CallTo(() => vippsCommerceService.FindCustomerContacts(testEmail, testPhoneNumber))
                .Returns(new[] { new CustomerContact() { UserId = testEmail }, new CustomerContact() { UserId = $"{testEmail}1" } });

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                vippsLoginService,
                vippsCommerceService,
                A.Fake<IVippsLoginSanityCheck>(),
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();

            await Assert.ThrowsAsync<VippsLoginDuplicateAccountException>(async () =>
                await notifications.DefaultSecurityTokenValidated(context));
        }

        private SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> CreateContext()
        {
            var options =
                new VippsOpenIdConnectAuthenticationOptions("clientId", "clientSecret", "authority");
            var context =
                new SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(
                    A.Fake<IOwinContext>(), options)
                {
                    AuthenticationTicket = new AuthenticationTicket(new ClaimsIdentity(), new AuthenticationProperties(
                        new Dictionary<string, string>()
                            {{".redirect", "https://test.url/redirect-url"}}))
                };
            return context;
        }

        private MapUserKey GetMapUserKey(string testEmail)
        {
            var mapUserKey = A.Fake<MapUserKey>();
            A.CallTo(() => mapUserKey.ToUserKey(testEmail)).Returns(testEmail);
            return mapUserKey;
        }
    }
}