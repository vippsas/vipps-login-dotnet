using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using Xunit;

namespace Vipps.Login.Tests
{
    public class VippsLoginServiceTests
    {
        [Fact]
        public void ThrowsArgumentNullException()
        {
            var service = new VippsLoginService();

            Assert.Throws<ArgumentNullException>(() => service.GetVippsUserInfo(null));
        }

        [Fact]
        public void NotIsVippsIdentityForUnknownIssuer()
        {
            var service = new VippsLoginService();

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(JwtClaimTypes.Issuer, "unknown"));
            Assert.False(service.IsVippsIdentity(identity));
        }

        [Fact]
        public void IsVippsIdentityForVippsIssuer()
        {
            var service = new VippsLoginService();

            var identity = new ClaimsIdentity();
            identity.AddClaim(new Claim(JwtClaimTypes.Issuer, VippsLoginService.VippsTestApi));
            Assert.True(service.IsVippsIdentity(identity));
        }

        [Fact]
        public void ValidateVippsIdentity()
        {
            var service = new VippsLoginService();

            Assert.True(service.IsVippsIdentity(CreateIdentity()));
        }

        [Fact]
        public void ReturnsNullIfNotVippsIdentity()
        {
            var service = new VippsLoginService();

            Assert.Null(service.GetVippsUserInfo(new ClaimsIdentity()));
        }

        [Fact]
        public void ReturnsNullIfNameIdentityMissing()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.RemoveClaim(identity.FindFirst(ClaimTypes.NameIdentifier));
            Assert.Null(service.GetVippsUserInfo(identity));
        }

        [Fact]
        public void HasTwoAddresses()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaims(new[] {_address1, _address2});
            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(2, userInfo.Addresses.Count());
            Assert.Equal(1, userInfo.Addresses.Count(x=>x.AddressType.Equals("home")));
            Assert.Equal(1, userInfo.Addresses.Count(x => x.AddressType.Equals("work")));
            Assert.Equal(0, userInfo.Addresses.Count(x => x.AddressType.Equals("other")));
        }

        [Fact]
        public void ParsesNorwegianDate()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaim(_birthDateClaim);

            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(new DateTime(2020, 6, 30), userInfo.BirthDate);
        }

        private ClaimsIdentity CreateIdentity()
        {
            var identity = new ClaimsIdentity();
            identity.AddClaims(new List<Claim>
            {
                _issuerClaim,
                _nameIdentifierClaim,
            });
            return identity;
        }

        private readonly Claim _issuerClaim =
            new Claim(JwtClaimTypes.Issuer, VippsLoginService.VippsTestApi);
        private readonly Claim _nameIdentifierClaim =
            new Claim(ClaimTypes.NameIdentifier, "3086A8D1-0AE2-4028-B5A8-D41628DDC9E8");
        private readonly Claim _birthDateClaim =
            new Claim(ClaimTypes.DateOfBirth, "30.6.2020");

        private readonly Claim _address1 = new Claim(JwtClaimTypes.Address, "{\"address_type\": \"home\",\"country\": \"NO\",\"formatted\": \"BOKS 6300, ETTERSTAD\n0603\nOSLO\nNO\",\"postal_code\": \"0603\",\"region\": \"OSLO\",\"street_address\": \"BOKS 6300, ETTERSTAD\"}");
        private readonly Claim _address2 = new Claim(JwtClaimTypes.Address, "{\"address_type\": \"work\",\"country\": \"NO\",\"formatted\": \"Skippergata 4\n0152\nOslo\nNO\",\"postal_code\": \"0152\",\"region\": \"Oslo\",\"street_address\": \"Skippergata 4\"}");
    }
}
