using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using EPiServer.Events.ChangeNotification.Implementation;
using EPiServer.Security;
using FakeItEasy;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Customers;
using Vipps.Login.Episerver.Commerce.Exceptions;
using Vipps.Login.Models;
using Xunit;

namespace Vipps.Login.Episerver.Commerce.Tests
{
    public class VippsLoginCommerceServiceTests
    {
        [Fact]
        public void FindCustomerContactShouldBeNull()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>());

            var contact = service.FindCustomerContact(Guid.Empty);
            Assert.Null(contact);
        }

        [Fact]
        public void FindCustomerContactShouldReturnContact()
        {
            var expectedContact = new CustomerContact();
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsBySubjectGuid(A<Guid>._))
                .Returns(new[] {expectedContact});
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader
            );

            var contact = service.FindCustomerContact(Guid.Empty);
            Assert.Equal(expectedContact, contact);
        }

        [Fact]
        public void FindCustomerContactShouldReturnFirstContact()
        {
            var expectedContact = new CustomerContact();
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsBySubjectGuid(A<Guid>._))
                .Returns(new[] {expectedContact, new CustomerContact()});
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader
            );

            var contact = service.FindCustomerContact(Guid.Empty);
            Assert.Equal(expectedContact, contact);
        }

        [Fact]
        public void FindCustomerContactShouldReturnNotEqualLastContact()
        {
            var expectedContact = new CustomerContact();
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsBySubjectGuid(A<Guid>._))
                .Returns(new[] {new CustomerContact(), expectedContact});
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader
            );

            var contact = service.FindCustomerContact(Guid.Empty);
            Assert.NotEqual(expectedContact, contact);
        }


        [Fact]
        public void FindCustomerContactsShouldBeEmpty()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>());

            var contact = service.FindCustomerContacts(string.Empty, string.Empty);
            Assert.Empty(contact);
        }

        [Fact]
        public void FindCustomerContactsShouldReturnByEmail()
        {
            var expectedContacts = new[]
            {
                new CustomerContact {PrimaryKeyId = new PrimaryKeyId(1)},
                new CustomerContact {PrimaryKeyId = new PrimaryKeyId(2)}
            };
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsByEmail(A<string>._))
                .Returns(expectedContacts);

            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader);

            var contacts = service.FindCustomerContacts(string.Empty, string.Empty);

            Assert.Equal(expectedContacts, contacts);
        }

        [Fact]
        public void FindCustomerContactsShouldReturnByPhone()
        {
            var expectedContacts = new[]
            {
                new CustomerContact {PrimaryKeyId = new PrimaryKeyId(1)},
                new CustomerContact {PrimaryKeyId = new PrimaryKeyId(2)}
            };
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsByPhone(A<string>._))
                .Returns(expectedContacts);

            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader);

            var contacts = service.FindCustomerContacts(string.Empty, string.Empty);

            Assert.Equal(expectedContacts, contacts);
        }

        [Fact]
        public void FindCustomerContactsShouldBeDistinct()
        {
            var expectedContacts = new[]
            {
                new CustomerContact {PrimaryKeyId = new PrimaryKeyId(1)},
                new CustomerContact {PrimaryKeyId = new PrimaryKeyId(2)}
            };
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsByEmail(A<string>._))
                .Returns(expectedContacts);
            A.CallTo(() => dataLoader.FindContactsByPhone(A<string>._))
                .Returns(expectedContacts);

            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader);

            var contacts = service.FindCustomerContacts(string.Empty, string.Empty);

            Assert.Equal(expectedContacts, contacts);
        }

        [Fact]
        public void SyncInfoShouldThrowOnNull()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>());

            Assert.Throws<ArgumentNullException>(() => service.SyncInfo(null, null));
            Assert.Throws<ArgumentNullException>(() => service.SyncInfo(new ClaimsIdentity(), null));
            Assert.Throws<ArgumentNullException>(() => service.SyncInfo(null, new CustomerContact()));
        }

        [Fact]
        public void SyncInfoShouldSyncUserInfo()
        {
            var customerContact = A.Fake<CustomerContact>();
            var userinfo = new VippsUserInfo()
            {
                Addresses = Enumerable.Empty<VippsAddress>()
            };
            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.GetVippsUserInfo(A<ClaimsIdentity>._)).Returns(userinfo);
            var mapper = A.Fake<IVippsLoginMapper>();
            var service = new VippsLoginCommerceService(
                loginService,
                mapper,
                A.Fake<IVippsLoginDataLoader>());

            service.SyncInfo(new ClaimsIdentity(), customerContact, new VippsSyncOptions
            {
                SyncContactInfo = true,
                SyncAddresses = false,
                ShouldSaveContact = false,
            });

            A.CallTo(() => mapper.MapVippsContactFields(A<CustomerContact>._, A<VippsUserInfo>._)).MustHaveHappened();
        }

        [Fact]
        public void SyncInfoShouldNotSyncUserInfo()
        {
            var userinfo = new VippsUserInfo()
            {
                Addresses = Enumerable.Empty<VippsAddress>()
            };
            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.GetVippsUserInfo(A<ClaimsIdentity>._)).Returns(userinfo);
            var mapper = A.Fake<IVippsLoginMapper>();
            var service = new VippsLoginCommerceService(
                loginService,
                mapper,
                A.Fake<IVippsLoginDataLoader>());

            service.SyncInfo(new ClaimsIdentity(), new CustomerContact(), new VippsSyncOptions
            {
                SyncContactInfo = false,
                SyncAddresses = false,
                ShouldSaveContact = false,
            });

            A.CallTo(() => mapper.MapVippsAddressFields(A<CustomerAddress>._, A<VippsAddress>._)).MustNotHaveHappened();
        }

        [Fact]
        public void SyncInfoShouldSyncAddressInfo()
        {
            var contact = A.Fake<CustomerContact>();

            var userInfo = new VippsUserInfo()
            {
                Sub = Guid.NewGuid(),
                Addresses = new[] {new VippsAddress(), new VippsAddress() }
            };
            var identity = new ClaimsIdentity();
            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.GetVippsUserInfo(A<IIdentity>._))
                .Returns(userInfo);

            var mapper = A.Fake<IVippsLoginMapper>();
            var service = new VippsLoginCommerceService(
                loginService,
                mapper,
                A.Fake<IVippsLoginDataLoader>());

            service.SyncInfo(identity, contact, new VippsSyncOptions
            {
                SyncContactInfo = false,
                SyncAddresses = true,
                ShouldSaveContact = false,
            });

            A.CallTo(() => mapper.MapAddress(null, CustomerAddressTypeEnum.Billing, null, String.Empty))
                .WithAnyArguments()
                .MustHaveHappenedTwiceExactly();
        }
    }
}