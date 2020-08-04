using System;
using System.Configuration;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Xunit;

namespace Vipps.Login.Tests
{
    public class VippsOpenIdConnectAuthenticationNotificationsTests
    {
        [Fact]
        public async Task RetrievesTokenEndpointResponse()
        {
            var httpClient = A.Fake<HttpClient>();
            A.CallTo(() => httpClient.SendAsync(A<ProtocolRequest>._, A<CancellationToken>._))
                .Returns(new HttpResponseMessage
                {
                    Content = new StringContent(
                        "{\"access_token\":\"abcde\",\"expires_in\":3599,\"id_token\":\"qwerty\",\"scope\":\"phoneNumber address email name openid birthDate\",\"token_type\":\"bearer\"}")
                });

            var notifications = new VippsOpenIdConnectAuthenticationNotifications(httpClient);

            var configurationManager =
                A.Fake<IConfigurationManager<OpenIdConnectConfiguration>>();
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._))
                .Returns(new OpenIdConnectConfiguration());

            var notification = new AuthorizationCodeReceivedNotification(
                A.Fake<IOwinContext>(),
                new VippsOpenIdConnectAuthenticationOptions("clientId", "clientSecret", "authority")
                {
                    ConfigurationManager = configurationManager
                })
            {
                RedirectUri = "https://redirect-url",
                Code = "AuthCode"
            };
            await notifications.AuthorizationCodeReceived(notification);

            Assert.Equal("abcde", notification.TokenEndpointResponse.AccessToken);
            Assert.Equal("qwerty", notification.TokenEndpointResponse.IdToken);
        }

        [Fact]
        public async Task ThrowsOnInvalidTokenResponse()
        {
            var httpClient = A.Fake<HttpClient>();
            A.CallTo(() => httpClient.SendAsync(A<ProtocolRequest>._, A<CancellationToken>._))
                .Returns(new HttpResponseMessage
                {
                    Content = new StringContent(
                        "xxxxxxxxxxxxxxxxx")
                });

            var notifications = new VippsOpenIdConnectAuthenticationNotifications(httpClient);

            var configurationManager =
                A.Fake<IConfigurationManager<OpenIdConnectConfiguration>>();
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._))
                .Returns(new OpenIdConnectConfiguration());

            var notification = new AuthorizationCodeReceivedNotification(
                A.Fake<IOwinContext>(),
                new VippsOpenIdConnectAuthenticationOptions("clientId", "clientSecret", "authority")
                {
                    ConfigurationManager = configurationManager
                })
            {
                RedirectUri = "https://redirect-url",
                Code = "AuthCode"
            };
            await Assert.ThrowsAsync<OpenIdConnectProtocolException>(async () =>
                await notifications.AuthorizationCodeReceived(notification));
        }

        [Fact]
        public async Task ThrowsIfNotUsingHttps()
        {
            var notifications = new VippsOpenIdConnectAuthenticationNotifications(A.Fake<HttpClient>());

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
            A.CallTo(() => request.Uri).ReturnsLazily(() => new Uri("http://test.com/asdf"));
            A.CallTo(() => context.Request).Returns(request);


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
            await Assert.ThrowsAsync<ConfigurationErrorsException>(async () =>
                await notifications.RedirectToIdentityProvider(notification));
        }

        [Fact]
        public async Task RedirectToIdentityProviderReturns403()
        {
            var notifications = new VippsOpenIdConnectAuthenticationNotifications(A.Fake<HttpClient>());

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
    }
}