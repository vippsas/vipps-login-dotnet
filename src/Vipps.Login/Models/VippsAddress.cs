using Newtonsoft.Json;

namespace Vipps.Login.Models
{
    public class VippsAddress
    {
        [JsonProperty("street_address")]
        public string StreetAddress { get; set; }
        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string Formatted { get; set; }
        [JsonProperty("address_type")]
        public string AddressType { get; set; }
    }
}