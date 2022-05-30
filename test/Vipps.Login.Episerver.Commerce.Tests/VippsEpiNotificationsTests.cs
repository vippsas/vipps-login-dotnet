using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EPiServer.Security;
using FakeItEasy;
using Mediachase.Commerce.Customers;
using Microsoft.IdentityModel.Protocols;
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

        [Fact]
        public async Task DefaultSecurityTokenValidatedThrowsIfNoUserIdFound()
        {
            var testEmail = "test@test.com";

            var vippsLoginService = A.Fake<IVippsLoginService>();

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

            var exception = await Assert.ThrowsAsync<VippsLoginException>(async () =>
                 await notifications.DefaultSecurityTokenValidated(context));
        }


        [Fact]
        public async Task DefaultSecurityTokenValidated_GetUserInfoClaims()
        {
            var testEmail = "test@test.com";

            var testClaimName = "testClaimName";
            var testClaimValue = "testClaimValue";

            var vippsLoginService = A.Fake<IVippsLoginService>();           
            A.CallTo(() => vippsLoginService.GetUserInfoClaims(A<string>._, A<string>._))
                .Returns(Task.FromResult<IEnumerable<Claim>>(new List<Claim>
                {
                    new Claim(testClaimName, testClaimValue)
                }));

            A.CallTo(() => vippsLoginService.GetVippsUserInfo(A<ClaimsIdentity>._))
                .Returns(new VippsUserInfo
                {
                    Email = testEmail
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

            A.CallTo(() => vippsLoginService.GetUserInfoClaims(A<string>._, A<string>._))
                .MustHaveHappenedOnceExactly();
            Assert.True(
                context.AuthenticationTicket.Identity.HasClaim(
                    testClaimName,
                    testClaimValue)
            );
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

        // Linked Account user
        // Account already linked
        // Find contact with matching subject guid
        [Fact]
        public async Task DefaultSecurityTokenValidatedThrowsLinkedAccountAlreadyExists()
        {
            var linkAccountGuid = Guid.NewGuid();
            var testEmail = "test@test.com";
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContactByLinkAccountToken(linkAccountGuid))
                .Returns(new CustomerContact() {UserId = testEmail});

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                vippsCommerceService,
                A.Fake<IVippsLoginSanityCheck>(),
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();
            context.AuthenticationTicket.Properties.Dictionary.Add(VippsConstants.LinkAccount,
                linkAccountGuid.ToString());

            var exception = await Assert.ThrowsAsync<VippsLoginLinkAccountException>(async () =>
                await notifications.DefaultSecurityTokenValidated(context));

            Assert.True(exception.UserError);
        }

        // Linked Account user
        // Find contact with matching subject guid
        [Fact]
        public async Task DefaultSecurityTokenValidatedSetsLinkedAccountEmailAsNameClaim()
        {
            var linkAccountGuid = Guid.NewGuid();
            var testEmail = "test@test.com";
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContactByLinkAccountToken(linkAccountGuid))
                .Returns(new CustomerContact() {UserId = testEmail});
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(null);

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                vippsCommerceService,
                A.Fake<IVippsLoginSanityCheck>(),
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();
            context.AuthenticationTicket.Properties.Dictionary.Add(VippsConstants.LinkAccount,
                linkAccountGuid.ToString());

            await notifications.DefaultSecurityTokenValidated(context);
            Assert.True(
                context.AuthenticationTicket.Identity.HasClaim(
                    context.AuthenticationTicket.Identity.NameClaimType,
                    testEmail)
            );
        }

        // Linked Account user
        // Throws if no linked account user found
        [Fact]
        public async Task DefaultSecurityTokenThrowsIfNoLinkedAccountFound()
        {
            var linkAccountGuid = Guid.NewGuid();
            var testEmail = "test@test.com";
            var vippsCommerceService = A.Fake<IVippsLoginCommerceService>();
            A.CallTo(() => vippsCommerceService.FindCustomerContactByLinkAccountToken(linkAccountGuid))
                .Returns(null);
            A.CallTo(() => vippsCommerceService.FindCustomerContact(A<Guid>._))
                .Returns(null);

            var notifications = new VippsEpiNotifications(
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                vippsCommerceService,
                A.Fake<IVippsLoginSanityCheck>(),
                GetMapUserKey(testEmail)
            );

            var context = CreateContext();
            context.AuthenticationTicket.Properties.Dictionary.Add(VippsConstants.LinkAccount,
                linkAccountGuid.ToString());

            var exception = await Assert.ThrowsAsync<VippsLoginLinkAccountException>(async () =>
                await notifications.DefaultSecurityTokenValidated(context));
            Assert.False(exception.UserError);
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
                .Returns(new[] {new CustomerContact {UserId = testEmail}});

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
                .Returns(new[] {new CustomerContact {UserId = testEmail}});

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
                .Returns(new[]
                    {new CustomerContact() {UserId = testEmail}, new CustomerContact() {UserId = $"{testEmail}1"}});

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

        [Fact]
        public async Task RedirectToIdentityProviderReturns403()
        {
            var notifications = new VippsEpiNotifications(
                A.Fake<HttpClient>(),
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginCommerceService>(),
                A.Fake<IVippsLoginSanityCheck>(),
                A.Fake<MapUserKey>());

            var configurationManager =
                A.Fake<IConfigurationManager<OpenIdConnectConfiguration>>();
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._))
                .Returns(new OpenIdConnectConfiguration());

            var context = A.Fake<IOwinContext>();
            var response = new OwinResponse()
            {
                StatusCode = 401
            };
            A.CallTo(() => context.Response).Returns(response);
            var request = A.Fake<IOwinRequest>();
            A.CallTo(() => request.Uri).ReturnsLazily(() => new Uri("https://test.com/asdf"));
            A.CallTo(() => context.Request).Returns(request);

            var user = A.Fake<ClaimsPrincipal>();
            A.CallTo(() => context.Authentication.User).Returns(user);
            A.CallTo(() => user.Identity.IsAuthenticated).Returns(true);

            var notification =
                new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(
                    context,
                    new VippsOpenIdConnectAuthenticationOptions("clientId", "clientSecret", "authority")
                    {
                        ConfigurationManager = configurationManager
                    })
                {
                    ProtocolMessage = new OpenIdConnectMessage()
                };
            await notifications.RedirectToIdentityProvider(notification);
            Assert.Equal(403, response.StatusCode);
        }

        [Fact]
        public async Task RedirectToIdentityProviderDoesNotReturn403ForLinkAccount()
        {
            var notifications = new VippsEpiNotifications(
                A.Fake<HttpClient>(),
                A.Fake<ISynchronizingUserService>(),
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginCommerceService>(),
                A.Fake<IVippsLoginSanityCheck>(),
                A.Fake<MapUserKey>());

            var configurationManager =
                A.Fake<IConfigurationManager<OpenIdConnectConfiguration>>();
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._))
                .Returns(new OpenIdConnectConfiguration());

            var context = A.Fake<IOwinContext>();
            var response = new OwinResponse()
            {
                StatusCode = 401
            };
            A.CallTo(() => context.Response).Returns(response);
            var request = A.Fake<IOwinRequest>();
            A.CallTo(() => request.Uri).ReturnsLazily(() => new Uri("https://test.com/asdf"));
            A.CallTo(() => context.Request).Returns(request);

            var properties = new AuthenticationProperties();
            properties.Dictionary.Add(VippsConstants.LinkAccount, VippsConstants.LinkAccount);
            A.CallTo(() => context.Authentication.AuthenticationResponseChallenge)
                .Returns(new AuthenticationResponseChallenge(new[] {""}, properties));

            var notification =
                new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(
                    context,
                    new VippsOpenIdConnectAuthenticationOptions("clientId", "clientSecret", "authority")
                    {
                        ConfigurationManager = configurationManager
                    })
                {
                    ProtocolMessage = new OpenIdConnectMessage()
                };

            await notifications.RedirectToIdentityProvider(notification);
            Assert.NotEqual(403, response.StatusCode);
        }

        private SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>
            CreateContext(
            string userInfoEndpoint = "https://user-info-endpoint",
            string scope = "")
        {
            var configurationManager =
                A.Fake<IConfigurationManager<OpenIdConnectConfiguration>>();
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._))
                .Returns(new OpenIdConnectConfiguration
                {
                    UserInfoEndpoint = "https://user-info-endpoint",
                });
            var options =
                new VippsOpenIdConnectAuthenticationOptions("clientId", "clientSecret", "authority")
                {
                    ConfigurationManager = configurationManager,
                    Scope = scope
                };
            var context =
                new SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(
                    A.Fake<IOwinContext>(), options)
                {
                    AuthenticationTicket = new AuthenticationTicket(new ClaimsIdentity(), new AuthenticationProperties(
                        new Dictionary<string, string>()
                            {{".redirect", "https://test.url/redirect-url"}})),
                    ProtocolMessage = A.Fake<OpenIdConnectMessage>()
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