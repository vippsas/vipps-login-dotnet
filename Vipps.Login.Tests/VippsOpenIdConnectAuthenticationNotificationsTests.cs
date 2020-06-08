using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Security.Notifications;
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
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._)).Returns(new OpenIdConnectConfiguration());

            var notification = new AuthorizationCodeReceivedNotification(
                A.Fake<IOwinContext>(),
                new VippsOpenIdConnectAuthenticationOptions
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
            A.CallTo(() => configurationManager.GetConfigurationAsync(A<CancellationToken>._)).Returns(new OpenIdConnectConfiguration());

            var notification = new AuthorizationCodeReceivedNotification(
                A.Fake<IOwinContext>(),
                new VippsOpenIdConnectAuthenticationOptions
                {
                    ConfigurationManager = configurationManager
                })
            {
                RedirectUri = "https://redirect-url",
                Code = "AuthCode"
            };
            await Assert.ThrowsAsync<OpenIdConnectProtocolException>(async () => await notifications.AuthorizationCodeReceived(notification));
        }
    }
}
