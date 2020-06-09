using System;
using System.Linq;
using Mediachase.Commerce.Customers;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class VippsOpenIdConnectAuthenticationNotificationsTests
    {
        [Fact]
        public void FindsNoAddressIfEmptyList()
        {
            var customerAddress = new CustomerAddress[0];
            
            var result = customerAddress.FindAllVippsAddresses();

            Assert.Empty(result);
        }

        [Fact]
        public void FindsNoAddressIfNoVippsAddress()
        {
            var customerAddress = new[] { CustomerAddress.CreateInstance() };

            var result = customerAddress.FindAllVippsAddresses();

            Assert.Empty(result);
        }

        [Fact]
        public void FindsOneAddressIfOneVippsAddress()
        {
            var customerAddress = new[] { CreateVippsAddress(VippsAddressType.Work) };

            var result = customerAddress.FindAllVippsAddresses();

            Assert.Equal(1, result.Count());
        }

        
        [Fact]
        public void FindsOneAddressIfOneVippsAddressAndOthers()
        {
            var customerAddress = new[] { CustomerAddress.CreateInstance(), CreateVippsAddress(VippsAddressType.Work), CustomerAddress.CreateInstance() };

            var result = customerAddress.FindAllVippsAddresses();

            Assert.Equal(1, result.Count());
        }

        [Fact]
        public void FindsWorkAddressIfWorkVippsAddressAndOthers()
        {
            var vippsAddress = CreateVippsAddress(VippsAddressType.Work);
            var customerAddress = new[]
            {
                CustomerAddress.CreateInstance(), CreateVippsAddress(VippsAddressType.Home), vippsAddress,
                CreateVippsAddress(VippsAddressType.Other), CustomerAddress.CreateInstance()
            };

            var result = customerAddress.FindVippsAddress(VippsAddressType.Work);

            Assert.Equal(vippsAddress, result);
        }

        [Fact]
        public void FindsFirstWorkAddressIfMultipleWorkAddresses()
        {
            var vippsAddress = CreateVippsAddress(VippsAddressType.Work);
            var customerAddress = new[]
            {
                vippsAddress,
                CreateVippsAddress(VippsAddressType.Work)
            };

            var result = customerAddress.FindVippsAddress(VippsAddressType.Work);

            Assert.Equal(vippsAddress, result);
        }

        [Fact]
        public void AddressTypeNullByDefault()
        {
            var customerAddress = CustomerAddress.CreateInstance();

            Assert.Null(customerAddress.GetVippsAddressType());
        }

        [Fact]
        public void AddressTypeNullIfSetToNull()
        {
            var customerAddress = CustomerAddress.CreateInstance();
            customerAddress.SetVippsAddressType(null);

            Assert.Null(customerAddress.GetVippsAddressType());
        }

        [Fact]
        public void AddressTypeNullIfSetToUnknownString()
        {
            var customerAddress = CustomerAddress.CreateInstance();
            customerAddress[MetadataConstants.VippsAddressTypeFieldName] = "unknown";

            Assert.Null(customerAddress.GetVippsAddressType());
        }

        [Fact]
        public void AddressTypeHomeIfSetToHome()
        {
            var customerAddress = CustomerAddress.CreateInstance();
            customerAddress.SetVippsAddressType(VippsAddressType.Home);

            Assert.Equal(VippsAddressType.Home, customerAddress.GetVippsAddressType());
        }

        [Fact]
        public void MapVippsAddressThrowsIfNull()
        {
            var customerAddress = CustomerAddress.CreateInstance();

            Assert.Throws<ArgumentNullException>(() => customerAddress.MapVippsAddress(null));
        }

        [Fact]
        public void MapVippsAddress()
        {
            var customerAddress = CustomerAddress.CreateInstance();

            var vippsAddressType = VippsAddressType.Work;
            var vippsStreetAddress = "Vipps Street Address";
            var region = "Oslo";
            var country = "NO";
            var postalCode = "0151";

            customerAddress.MapVippsAddress(new VippsAddress
            {
                AddressType = vippsAddressType,
                StreetAddress = vippsStreetAddress,
                Region = region,
                Country = country,
                PostalCode = postalCode
            });

            Assert.Equal(vippsAddressType, customerAddress.GetVippsAddressType());
            Assert.Equal(vippsStreetAddress, customerAddress.Line1);
            Assert.Equal(region, customerAddress.City);
            Assert.Equal(country, customerAddress.CountryCode);
            Assert.Equal(postalCode, customerAddress.PostalCode);
        }
        private CustomerAddress CreateVippsAddress(VippsAddressType addressType)
        {
            var vippsAddress = CustomerAddress.CreateInstance();
            vippsAddress.SetVippsAddressType(addressType);
            return vippsAddress;
        }

    }
}
