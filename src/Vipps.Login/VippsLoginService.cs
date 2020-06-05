using System;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using Vipps.Login.Models;

namespace Vipps.Login
{
    public class VippsLoginService : IVippsLoginService
    {
        private const string NorwegianLanguageTag = "no";

        public virtual VippsUserInfo GetVippsUserInfo(ClaimsIdentity identity)
        {
            var issuer = identity.FindFirst(JwtClaimTypes.Issuer);
            if (issuer == null || !issuer.Value.StartsWith(ConfigurationManager.AppSettings["VippsLogin:Authority"]))
            {
                throw new Exception("Invalid issuer. This is not a Vipps identity");
            }

            var subjectClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (subjectClaim == null || !Guid.TryParse(subjectClaim.Value, out var subject))
            {
                return null;
            }

            return new VippsUserInfo
            {
                Sub = subject,
                Addresses = identity.GetVippsAddresses(),
                BirthDate = ParseDate(identity.FindFirst(ClaimTypes.DateOfBirth)?.Value),
                Email = identity.FindFirst(ClaimTypes.Email)?.Value,
                EmailVerified =
                    bool.TryParse(identity.FindFirst(JwtClaimTypes.EmailVerified)?.Value, out var verified) && verified,
                FamilyName = identity.FindFirst(ClaimTypes.Surname)?.Value,
                GivenName = identity.FindFirst(ClaimTypes.GivenName)?.Value,
                Name = identity.FindFirst(JwtClaimTypes.Name)?.Value,
                PhoneNumber = identity.FindFirst(JwtClaimTypes.PhoneNumber)?.Value,
                Nnin = identity.FindFirst("nnin")?.Value
            };
        }

        protected virtual DateTime ParseDate(string dateString)
        {
            DateTime.TryParse(dateString,
                CultureInfo.GetCultureInfoByIetfLanguageTag(NorwegianLanguageTag), DateTimeStyles.None,
                out var date);
            return date;
        }
    }
}