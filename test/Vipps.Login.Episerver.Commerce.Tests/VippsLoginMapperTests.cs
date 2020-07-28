using System;
using Mediachase.Commerce.Customers;
using Vipps.Login.Episerver.Commerce.Extensions;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class VippsLoginMapperTests
    {
        [Fact]
        public void MapVippsContactFieldsThrowsIfVippsInfoIsNull()
        {
            var mapper = new VippsLoginMapper();

            var contact = CustomerContact.CreateInstance();

            Assert.Throws<ArgumentNullException>(() => mapper.MapVippsContactFields(contact, null));
        }

        [Fact]
        public void MapVippsContactFieldsThrowsIfContactIsNull()
        {
            var mapper = new VippsLoginMapper();


            Assert.Throws<ArgumentNullException>(() => mapper.MapVippsContactFields(null, new VippsUserInfo()));
        }

        [Fact]
        public void MapVippsContactFields()
        {
            var mapper = new VippsLoginMapper();

            var contact = CustomerContact.CreateInstance();

            var subject = Guid.NewGuid();
            var email = "test@geta.no";
            var givenName = "Test";
            var familyName = "Tester";
            var fullName = "Test Tester";
            var birthDate = DateTime.Now;

            mapper.MapVippsContactFields(contact, new VippsUserInfo
            {
                Sub = subject,
                Email = email,
                GivenName = givenName,
                FamilyName = familyName,
                Name = fullName,
                BirthDate = birthDate
            });

            Assert.Equal(email, contact.Email);
            Assert.Equal(givenName, contact.FirstName);
            Assert.Equal(familyName, contact.LastName);
            Assert.Equal(fullName, contact.FullName);
            Assert.Equal(birthDate, contact.BirthDate);
        }

        [Fact]
        public void MapVippsAddressFieldsThrowsIfVippsInfoIsNull()
        {
            var mapper = new VippsLoginMapper();

            var customerAddress = CustomerAddress.CreateInstance();

            Assert.Throws<ArgumentNullException>(() => mapper.MapVippsAddressFields(customerAddress, null));
        }

        [Fact]
        public void MapVippsAddressFieldsThrowsIfAddressIsNull()
        {
            var mapper = new VippsLoginMapper();

            Assert.Throws<ArgumentNullException>(() => mapper.MapVippsAddressFields(null, new VippsAddress()));
        }

        [Fact]
        public void MapVippsAddress()
        {
            var mapper = new VippsLoginMapper();

            var customerAddress = CustomerAddress.CreateInstance();

            var vippsAddressType = VippsAddressType.Work;
            var vippsStreetAddress = "Vipps Street Address";
            var region = "Oslo";
            var country = "NO";
            var postalCode = "0151";

            mapper.MapVippsAddressFields(customerAddress, new VippsAddress
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
    }
}