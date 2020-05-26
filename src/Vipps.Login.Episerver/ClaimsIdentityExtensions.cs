using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using EPiServer.Logging;
using IdentityModel;
using Newtonsoft.Json;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver
{
    public static class ClaimsIdentityExtensions
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(ClaimsIdentityExtensions));
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

            try
            {
                return JsonConvert.DeserializeObject<VippsAddress>(claim.Value);
            }
            catch (JsonException ex)
            {
                _logger.Debug("Can't deserialize address claim", ex);
                return null;
            }
        }
    }
}