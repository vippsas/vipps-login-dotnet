using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using IdentityModel;
using Newtonsoft.Json;
using Vipps.Login.Models;

namespace Vipps.Login
{
    public static class ClaimsIdentityExtensions
    {
        public static IEnumerable<VippsAddress> GetVippsAddresses(this ClaimsIdentity identity)
        {
            return identity
                .FindAll(JwtClaimTypes.Address)
                .Select(DeserializeAddress);
        }

        private static VippsAddress DeserializeAddress(Claim claim)
        {
            if (string.IsNullOrWhiteSpace(claim?.Value))
            {
                return null;
            }
            return JsonConvert.DeserializeObject<VippsAddress>(claim.Value);
        }
    }
}