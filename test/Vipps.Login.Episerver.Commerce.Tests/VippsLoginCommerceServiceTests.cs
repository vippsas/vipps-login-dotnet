using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using FakeItEasy;
using Mediachase.BusinessFoundation.Data;
using Mediachase.Commerce.Customers;
using Microsoft.Owin;
using Microsoft.Owin.Security;
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
                A.Fake<IVippsLoginDataLoader>(), A.Fake<ICustomerContactService>());

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
                dataLoader,
                A.Fake<ICustomerContactService>()
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
                dataLoader,
                A.Fake<ICustomerContactService>()
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
                dataLoader,
                A.Fake<ICustomerContactService>()
            );

            var contact = service.FindCustomerContact(Guid.Empty);
            Assert.NotEqual(expectedContact, contact);
        }

        [Fact]
        public void FindCustomerContactByLinkAccountTokenShouldBeNull()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(), A.Fake<ICustomerContactService>());

            var contact = service.FindCustomerContactByLinkAccountToken(Guid.Empty);
            Assert.Null(contact);
        }

        [Fact]
        public void FindCustomerContactByLinkAccountTokenShouldReturnContact()
        {
            var expectedContact = new CustomerContact();
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsByLinkAccountToken(A<Guid>._))
                .Returns(new[] {expectedContact});
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader,
                A.Fake<ICustomerContactService>()
            );

            var contact = service.FindCustomerContactByLinkAccountToken(Guid.Empty);
            Assert.Equal(expectedContact, contact);
        }

        [Fact]
        public void FindCustomerContactByLinkAccountTokenShouldReturnFirstContact()
        {
            var expectedContact = new CustomerContact();
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsByLinkAccountToken(A<Guid>._))
                .Returns(new[] {expectedContact, new CustomerContact()});
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader,
                A.Fake<ICustomerContactService>()
            );

            var contact = service.FindCustomerContactByLinkAccountToken(Guid.Empty);
            Assert.Equal(expectedContact, contact);
        }

        [Fact]
        public void FindCustomerContactByLinkAccountTokenShouldReturnNotEqualLastContact()
        {
            var expectedContact = new CustomerContact();
            var dataLoader = A.Fake<IVippsLoginDataLoader>();
            A.CallTo(() => dataLoader.FindContactsByLinkAccountToken(A<Guid>._))
                .Returns(new[] {new CustomerContact(), expectedContact});
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                dataLoader,
                A.Fake<ICustomerContactService>()
            );

            var contact = service.FindCustomerContactByLinkAccountToken(Guid.Empty);
            Assert.NotEqual(expectedContact, contact);
        }


        [Fact]
        public void FindCustomerContactsShouldBeEmpty()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

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
                dataLoader,
                A.Fake<ICustomerContactService>());

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
                dataLoader,
                A.Fake<ICustomerContactService>());

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
                dataLoader,
                A.Fake<ICustomerContactService>());

            var contacts = service.FindCustomerContacts(string.Empty, string.Empty);

            Assert.Equal(expectedContacts, contacts);
        }

        [Fact]
        public void SyncInfoShouldThrowOnNull()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

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
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            service.SyncInfo(new ClaimsIdentity(), customerContact, new VippsSyncOptions
            {
                SyncContactInfo = true,
                SyncAddresses = false,
                ShouldSaveContact = false,
            });

            A.CallTo(() => mapper.MapVippsContactFields(A<CustomerContact>._, A<VippsUserInfo>._)).MustHaveHappened();
        }

        [Fact]
        public void SyncInfoShouldSaveChanges()
        {
            var customerContact = A.Fake<CustomerContact>();
            var userinfo = new VippsUserInfo()
            {
                Addresses = Enumerable.Empty<VippsAddress>()
            };
            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.GetVippsUserInfo(A<ClaimsIdentity>._)).Returns(userinfo);

            var customerContactService = A.Fake<ICustomerContactService>();
            var mapper = A.Fake<IVippsLoginMapper>();
            var service = new VippsLoginCommerceService(
                loginService,
                mapper,
                A.Fake<IVippsLoginDataLoader>(),
                customerContactService);

            service.SyncInfo(new ClaimsIdentity(), customerContact, new VippsSyncOptions
            {
                SyncContactInfo = false,
                SyncAddresses = false,
                ShouldSaveContact = true,
            });

            A.CallTo(() => customerContactService.SaveChanges(A<CustomerContact>._)).MustHaveHappened();
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
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

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
                Addresses = new[] {new VippsAddress(), new VippsAddress()}
            };
            var identity = new ClaimsIdentity();
            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.GetVippsUserInfo(A<IIdentity>._))
                .Returns(userInfo);

            var mapper = A.Fake<IVippsLoginMapper>();
            var service = new VippsLoginCommerceService(
                loginService,
                mapper,
                A.Fake<IVippsLoginDataLoader>(), A.Fake<ICustomerContactService>());

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

        [Fact]
        public void HandleLoginUnauthorizedUserReturnsTrue()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            var context = A.Fake<IOwinContext>();
            Assert.True(service.HandleLogin(context));

            A.CallTo(() => context.Authentication.Challenge(VippsAuthenticationDefaults.AuthenticationType))
                .MustHaveHappened();
        }

        [Fact]
        public void HandleLoginAuthorizedUserReturnsFalse()
        {
            var context = A.Fake<IOwinContext>();
            var user = A.Fake<ClaimsPrincipal>();
            A.CallTo(() => context.Authentication.User).Returns(user);
            A.CallTo(() => user.Identity.IsAuthenticated).Returns(true);

            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            Assert.False(service.HandleLogin(
                context,
                new VippsSyncOptions {ShouldSaveContact = false}, customerContact:
                new CustomerContact()));

            A.CallTo(() => context.Authentication.Challenge(VippsAuthenticationDefaults.AuthenticationType))
                .MustNotHaveHappened();
        }

        [Fact]
        public void HandleHandleLinkAccountThrowsForNonAuthenticatedUser()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            Assert.Throws<InvalidOperationException>(() => service.HandleLinkAccount(A.Fake<IOwinContext>()));
        }

        [Fact]
        public void HandleHandleLinkAccountReturnsFalseForVippsUser()
        {
            var context = A.Fake<IOwinContext>();
            var user = A.Fake<ClaimsPrincipal>();
            A.CallTo(() => context.Authentication.User).Returns(user);
            A.CallTo(() => user.Identity.IsAuthenticated).Returns(true);

            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.IsVippsIdentity(A<IIdentity>._)).Returns(true);

            var service = new VippsLoginCommerceService(
                loginService,
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            Assert.False(service.HandleLinkAccount(context));
        }

        [Fact]
        public void HandleHandleLinkAccountReturnsTrueForNonVippsUser()
        {
            var context = A.Fake<IOwinContext>();
            var user = A.Fake<ClaimsPrincipal>();
            A.CallTo(() => context.Authentication.User).Returns(user);
            A.CallTo(() => user.Identity.IsAuthenticated).Returns(true);

            var loginService = A.Fake<IVippsLoginService>();
            A.CallTo(() => loginService.IsVippsIdentity(A<IIdentity>._)).Returns(false);

            var customerContactService = A.Fake<ICustomerContactService>();

            var service = new VippsLoginCommerceService(
                loginService,
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                customerContactService);

            Assert.True(service.HandleLinkAccount(context, new CustomerContact()));

            A.CallTo(() =>
                    context.Authentication.Challenge(A<AuthenticationProperties>._,
                        VippsAuthenticationDefaults.AuthenticationType))
                .WhenArgumentsMatch((args) =>
                {
                    // Verify link account token
                    var props = args[0] as AuthenticationProperties;
                    if (props?.Dictionary == null ||
                        !props.Dictionary.ContainsKey(VippsConstants.LinkAccount) ||
                        !Guid.TryParse(props.Dictionary[VippsConstants.LinkAccount], out _))
                    {
                        return false;
                    }

                    // Verify auth type
                    if (!(args[1] is string[] types) ||
                        !types.Contains(VippsAuthenticationDefaults.AuthenticationType))
                    {
                        return false;
                    }

                    return true;
                })
                .MustHaveHappened();

            // Verify storing link account token on account
            A.CallTo(() => customerContactService.SaveChanges(A<CustomerContact>._))
                .MustHaveHappened();
        }

        [Fact]
        public void HandleRedirect()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            var context = A.Fake<IOwinContext>();

            Assert.True(service.HandleRedirect(context, "/redirect-url"));

            A.CallTo(() => context.Response.Redirect("/redirect-url")).MustHaveHappened();
        }

        [Fact]
        public void HandleRedirectToLocalUrlOnly()
        {
            var service = new VippsLoginCommerceService(
                A.Fake<IVippsLoginService>(),
                A.Fake<IVippsLoginMapper>(),
                A.Fake<IVippsLoginDataLoader>(),
                A.Fake<ICustomerContactService>());

            var context = A.Fake<IOwinContext>();
            
            Assert.True(service.HandleRedirect(context, "https://test.url/redirect-url"));

            A.CallTo(() => context.Response.Redirect("/redirect-url")).MustHaveHappened();
        }
    }
}