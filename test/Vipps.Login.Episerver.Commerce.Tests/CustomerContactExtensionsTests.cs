using System;
using Mediachase.Commerce.Customers;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class CustomerContactExtensionsTests
    {
        [Fact]
        public void SubjectNullByDefault()
        {
            var contact = CustomerContact.CreateInstance();

            Assert.Null(contact.GetVippsSubject());
        }

        [Fact]
        public void SubjectNullIfSetToNull()
        {
            var contact = CustomerContact.CreateInstance();
            contact.SetVippsSubject(null);

            Assert.Null(contact.GetVippsSubject());
        }

        [Fact]
        public void SubjectNullIfSetToInvalidGuid()
        {
            var contact = CustomerContact.CreateInstance();
            contact[MetadataConstants.VippsSubjectGuidFieldName] = "xxx";

            Assert.Null(contact.GetVippsSubject());
        }

        [Fact]
        public void CanSetAndGetSubjectGuid()
        {
            var contact = CustomerContact.CreateInstance();
            var guid = Guid.NewGuid();
            contact.SetVippsSubject(guid);

            Assert.Equal(guid, contact.GetVippsSubject());
        }

        [Fact]
        public void MapVippsUserInfoThrowsIfNull()
        {
            var contact = CustomerContact.CreateInstance();

            Assert.Throws<ArgumentNullException>(() => contact.MapVippsUserInfo(null));
        }

        [Fact]
        public void MapVippsUserInfo()
        {
            var contact = CustomerContact.CreateInstance();

            var subject = Guid.NewGuid();
            var email = "test@geta.no";
            var givenName = "Test";
            var familyName = "Tester";
            var fullName = "Test Tester";
            var birthDate = DateTime.Now;

            contact.MapVippsUserInfo(new VippsUserInfo
            {
                Sub = subject,
                Email = email,
                GivenName = givenName,
                FamilyName = familyName,
                Name = fullName,
                BirthDate = birthDate
            });

            Assert.Equal(subject, contact.GetVippsSubject());
            Assert.Equal(email, contact.Email);
            Assert.Equal(givenName, contact.FirstName);
            Assert.Equal(familyName, contact.LastName);
            Assert.Equal(fullName, contact.FullName);
            Assert.Equal(birthDate, contact.BirthDate);
        }
    }
}
