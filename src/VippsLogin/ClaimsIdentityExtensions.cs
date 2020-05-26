using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Epi.VippsLogin.Models;
using Newtonsoft.Json;

namespace Epi.VippsLogin
{
    public static class ClaimsIdentityExtensions
    {
        public static IEnumerable<VippsAddress> GetVippsAddresses(this ClaimsIdentity identity)
        {
            return identity
                .FindAll(VippsScope.Address)
                .Select(DeserializeAddress);
        }

        private static VippsAddress DeserializeAddress(Claim claim)
        {
            if (string.IsNullOrWhiteSpace(claim?.Value))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<VippsAddress>(claim.Value);
            }
            catch (JsonException ex)
            {
                return null;
            }
        }
    }
}