using System;
using System.Collections.Generic;
using System.Security.Principal;
using Mediachase.Commerce.Customers;
using Microsoft.Owin;

namespace Vipps.Login.Episerver.Commerce
{
    public interface IVippsLoginCommerceService
    {
        CustomerContact FindCustomerContact(Guid subjectGuid);
        IEnumerable<CustomerContact> FindCustomerContacts(string email, string phone);
        void SyncInfo(IIdentity identity, CustomerContact currentContact, VippsSyncOptions options = default);
        Guid CreateLinkAccountToken(CustomerContact contact);
        CustomerContact FindCustomerContactByLinkAccountToken(Guid linkAccountToken);
        bool HandleLogin(IOwinContext context, VippsSyncOptions vippsSyncOptions = default, CustomerContact customerContact = null);
        bool HandleLinkAccount(IOwinContext context, CustomerContact customerContact = null);
        bool HandleRedirect(IOwinContext context, string returnUrl);
    }
}