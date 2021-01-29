using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using Vipps.Login.Models;
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
        public void NotIsVippsIdentityForNullIdentity()
        {
            var service = new VippsLoginService();
            Assert.False(service.IsVippsIdentity(null));
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
        public void HasThreeAddresses()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaims(new[] {_address1, _address2, _address3});
            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(3, userInfo.Addresses.Count());
            Assert.Equal(1, userInfo.Addresses.Count(x=>x.AddressType.Equals(VippsAddressType.Home)));
            Assert.Equal(1, userInfo.Addresses.Count(x => x.AddressType.Equals(VippsAddressType.Work)));
            Assert.Equal(1, userInfo.Addresses.Count(x => x.AddressType.Equals(VippsAddressType.Other)));
        }

        [Fact]
        public void ParsesBirthDate()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaim(_birthDateClaim);

            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(new DateTime(2020, 1, 2), userInfo.BirthDate);
        }

        [Fact]
        public void ParsesBirthDate2()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaim(_birthDateClaim2);

            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(new DateTime(2020, 6,30), userInfo.BirthDate);
        }

        [Fact]
        public void ParsesNorwegianBirthDate()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaim(_birthDateClaimNo);

            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(new DateTime(2020, 1, 2), userInfo.BirthDate);
        }

        [Fact]
        public void ParsesNorwegianBirthDate2()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            identity.AddClaim(_birthDateClaimNo2);

            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(new DateTime(2020, 6, 30), userInfo.BirthDate);
        }

        [Fact]
        public void MapsClaims()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();
            
            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(_subClaim.Value.ToLowerInvariant(), userInfo.Sub.ToString().ToLowerInvariant());
            Assert.Equal(_emailClaim.Value, userInfo.Email);
            Assert.Equal(_familyNameClaim.Value, userInfo.FamilyName);
            Assert.Equal(_givenNameClaim.Value, userInfo.GivenName);
            Assert.Equal(_nameClaim.Value, userInfo.Name);
            Assert.Equal(_phoneNumberClaim.Value, userInfo.PhoneNumber);
        }

        [Fact]
        public void PrefersJwtClaims()
        {
            var service = new VippsLoginService();
            var identity = CreateIdentity();

            identity.AddClaims(new []
            {
                _subClaimJwt,
                _emailClaimJwt,
                _familyNameClaimJwt,
                _givenNameClaimJwt,
                _nameClaimJwt,
                _phoneNumberClaimJwt,
            });

            var userInfo = service.GetVippsUserInfo(identity);
            Assert.Equal(_subClaimJwt.Value, userInfo.Sub.ToString());
            Assert.Equal(_emailClaimJwt.Value, userInfo.Email);
            Assert.Equal(_familyNameClaimJwt.Value, userInfo.FamilyName);
            Assert.Equal(_givenNameClaimJwt.Value, userInfo.GivenName);
            Assert.Equal(_nameClaimJwt.Value, userInfo.Name);
            Assert.Equal(_phoneNumberClaimJwt.Value, userInfo.PhoneNumber);
        }

        private ClaimsIdentity CreateIdentity()
        {
            var identity = new ClaimsIdentity();
            identity.AddClaims(new List<Claim>
            {
                _issuerClaim,
                _subClaim,
                _emailClaim,
                _familyNameClaim,
                _givenNameClaim,
                _nameClaim,
                _phoneNumberClaim,
                _nninClaim
            });
            return identity;
        }

        private readonly Claim _issuerClaim = CreateClaim(JwtClaimTypes.Issuer, VippsLoginService.VippsTestApi);
        private readonly Claim _subClaim = CreateClaim(ClaimTypes.NameIdentifier, "3086A8D1-0AE2-4028-B5A8-D41628DDC9E8");
        private readonly Claim _birthDateClaim = CreateClaim(ClaimTypes.DateOfBirth, "2020-01-02");
        private readonly Claim _birthDateClaim2 = CreateClaim(ClaimTypes.DateOfBirth, "2020-06-30");
        private readonly Claim _birthDateClaimNo = CreateClaim(ClaimTypes.DateOfBirth, "2.1.2020");
        private readonly Claim _birthDateClaimNo2 = CreateClaim(ClaimTypes.DateOfBirth, "30.6.2020");
        private readonly Claim _emailClaim = CreateClaim(ClaimTypes.Email);
        private readonly Claim _familyNameClaim = CreateClaim(ClaimTypes.Surname);
        private readonly Claim _givenNameClaim = CreateClaim(ClaimTypes.GivenName);
        private readonly Claim _nameClaim = CreateClaim(ClaimTypes.Name);
        private readonly Claim _phoneNumberClaim = CreateClaim(ClaimTypes.HomePhone);
        private readonly Claim _nninClaim = CreateClaim(VippsClaimTypes.Nnin);

        private readonly Claim _subClaimJwt = CreateClaim(JwtClaimTypes.Subject, Guid.NewGuid().ToString());
        private readonly Claim _emailClaimJwt = CreateClaim(JwtClaimTypes.Email);
        private readonly Claim _familyNameClaimJwt = CreateClaim(JwtClaimTypes.FamilyName);
        private readonly Claim _givenNameClaimJwt = CreateClaim(JwtClaimTypes.GivenName);
        private readonly Claim _nameClaimJwt = CreateClaim(JwtClaimTypes.Name);
        private readonly Claim _phoneNumberClaimJwt = CreateClaim(JwtClaimTypes.PhoneNumber);

        private static Claim CreateClaim(string claimType, string claimValue = null)
        {
            return new Claim(claimType, claimValue ?? claimType);
        }

        private readonly Claim _address1 = new Claim(JwtClaimTypes.Address, "{\"address_type\": \"home\",\"country\": \"NO\",\"formatted\": \"BOKS 6300, ETTERSTAD\n0603\nOSLO\nNO\",\"postal_code\": \"0603\",\"region\": \"OSLO\",\"street_address\": \"BOKS 6300, ETTERSTAD\"}");
        private readonly Claim _address2 = new Claim(JwtClaimTypes.Address, "{\"address_type\": \"work\",\"country\": \"NO\",\"formatted\": \"Skippergata 4\n0152\nOslo\nNO\",\"postal_code\": \"0152\",\"region\": \"Oslo\",\"street_address\": \"Skippergata 4\"}");
        private readonly Claim _address3 = new Claim(JwtClaimTypes.Address, "{\"address_type\":\"other\",\"country\":\"NO\",\"formatted\":\"Rådhusgata 28\nBar 3\n0151\nOslo\nNO\",\"postal_code\":\"0151\",\"region\":\"Oslo\",\"street_address\":\"Rådhusgata 28\nBar 3\"}");
    }
}
