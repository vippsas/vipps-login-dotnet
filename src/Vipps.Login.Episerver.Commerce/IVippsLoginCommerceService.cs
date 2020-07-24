using System;
using System.Collections.Generic;
using System.Security.Principal;
using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce
{
    public interface IVippsLoginCommerceService
    {
        IEnumerable<CustomerContact> FindCustomerContacts(Guid subjectGuid);
        IEnumerable<CustomerContact> FindCustomerContacts(string email, string phone);
        void SyncInfo(IIdentity identity, CustomerContact currentContact, VippsSyncOptions options = default);
    }
}