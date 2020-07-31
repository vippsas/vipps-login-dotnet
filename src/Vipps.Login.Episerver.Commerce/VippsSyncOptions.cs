using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsSyncOptions
    {
        public bool SyncContactInfo { get; set; } = true;
        public bool SyncAddresses { get; set; } = true;
        public bool ShouldSaveContact { get; set; } = true;

        public CustomerAddressTypeEnum AddressType { get; set; } =
            CustomerAddressTypeEnum.Shipping | CustomerAddressTypeEnum.Billing;
    }
}