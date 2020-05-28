using System;
using System.Collections.Generic;
using IdentityModel;
using Newtonsoft.Json;

namespace Vipps.Login.Models
{

    public class VippsUserInfo
    {
        public Guid Sub { get; set; }

        [JsonProperty(JwtClaimTypes.Address)]
        public IEnumerable<VippsAddress> Addresses { get; set; }
        
        [JsonProperty(JwtClaimTypes.BirthDate)]
        public DateTime? BirthDate { get; set; }

        public string Email { get; set; }
        
        [JsonProperty(JwtClaimTypes.EmailVerified)]
        public bool? EmailVerified { get; set; }
        
        [JsonProperty(JwtClaimTypes.FamilyName)]
        public string FamilyName { get; set; }
        
        [JsonProperty(JwtClaimTypes.GivenName)]
        public string GivenName { get; set; }
        
        public string Name { get; set; }
        
        [JsonProperty(JwtClaimTypes.PhoneNumber)]
        public string PhoneNumber { get; set; }

        public string Nnin { get; set; }
    }
}