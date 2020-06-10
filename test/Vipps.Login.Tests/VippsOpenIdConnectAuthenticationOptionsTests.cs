using System;
using Xunit;

namespace Vipps.Login.Tests
{
    public class VippsOpenIdConnectAuthenticationOptionsTests
    {
        [Theory]
        [InlineData(null, "clientSecret", "authority")]
        [InlineData("", "clientSecret", "authority")]
        [InlineData("clientId", null, "authority")]
        [InlineData("clientId", "", "authority")]
        [InlineData("clientId", "clientSecret", null)]
        [InlineData("clientId", "clientSecret", "")]
        public void VippsOpenIdConnectAuthenticationOptionsThrowsIfNullOrEmpty(string clientId, string clientSecret, string authority)
        {
            Assert.Throws<ArgumentException>(() =>
                new VippsOpenIdConnectAuthenticationOptions(clientId, clientSecret, authority));
        }

        [Theory]
        [InlineData("clientId", "clientSecret", "authority")]
        public void VippsOpenIdConnectAuthenticationOptionsDoesNotThrow(string clientId, string clientSecret, string authority)
        {
            var options = new VippsOpenIdConnectAuthenticationOptions(clientId, clientSecret, authority);

            Assert.Equal(clientId, options.ClientId);
            Assert.Equal(clientSecret, options.ClientSecret);
            Assert.Equal(authority, options.Authority);
        }

    }
}
