using System;
using Mediachase.Commerce.Customers;
using Vipps.Login.Episerver.Commerce.Extensions;
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
    }
}