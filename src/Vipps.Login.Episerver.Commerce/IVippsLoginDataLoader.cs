using System;
using System.Collections.Generic;
using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce
{
    public interface IVippsLoginDataLoader
    {
        IEnumerable<CustomerContact> FindContactsBySubjectGuid(Guid subjectGuid);
        IEnumerable<CustomerContact> FindContactsByLinkAccountToken(Guid linkAccountToken);
        IEnumerable<CustomerContact> FindContactsByEmail(string email);
        IEnumerable<CustomerContact> FindContactsByPhone(string phone);
    }
}