using System.Threading.Tasks;
using Mediachase.Commerce.Customers;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class VippsLoginSanityCheckTests
    {
        [Fact]
        public void IsValidContactFalseIfInputIsNull()
        {
            var sanityCheck = new VippsLoginSanityCheck();

            Assert.False(sanityCheck.IsValidContact(null, null));
            Assert.False(sanityCheck.IsValidContact(new CustomerContact(), null));
            Assert.False(sanityCheck.IsValidContact(null, new VippsUserInfo()));
        }

        [Fact]
        public void IsValidContactTrueIfFirstAndLastNameMatch()
        {
            var sanityCheck = new VippsLoginSanityCheck();

            var firstName = "firstName";
            var lastName = "lastName";
            var contact = new CustomerContact
            {
                FirstName = firstName,
                LastName = lastName
            };
            var userInfo = new VippsUserInfo
            {
                GivenName = firstName,
                FamilyName = lastName
            };

            Assert.True(sanityCheck.IsValidContact(contact, userInfo));
        }

        [Fact]
        public void IsValidContactFalseIfFirstAndLastNameDoNotMatch()
        {
            var sanityCheck = new VippsLoginSanityCheck();

            var firstName = "firstName";
            var lastName = "lastName";
            var contact = new CustomerContact
            {
                FirstName = firstName,
                LastName = lastName
            };
            var userInfo = new VippsUserInfo
            {
                GivenName = "xzxczcxz",
                FamilyName = "asdasdds"
            };

            Assert.False(sanityCheck.IsValidContact(contact, userInfo));
        }
    }
}
