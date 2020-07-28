using System;
using System.Linq;
using Mediachase.Commerce.Customers;
using Vipps.Login.Episerver.Commerce.Extensions;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class AddressExtensionsTests
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

       
        private CustomerAddress CreateVippsAddress(VippsAddressType addressType)
        {
            var vippsAddress = CustomerAddress.CreateInstance();
            vippsAddress.SetVippsAddressType(addressType);
            return vippsAddress;
        }
    }
}
