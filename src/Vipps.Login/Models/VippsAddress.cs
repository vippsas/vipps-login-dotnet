using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        [JsonConverter(typeof(StringEnumConverter))]
        public VippsAddressType AddressType { get; set; }
    }
}