using System;
using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce.Extensions
{
    public static class CustomerContactExtensions
    {
        public static void SetVippsSubject(this CustomerContact contact, Guid? subjectGuid)
        {
            contact[MetadataConstants.VippsSubjectGuidFieldName] = subjectGuid;
        }

        public static Guid? GetVippsSubject(this CustomerContact contact)
        {
            return contact[MetadataConstants.VippsSubjectGuidFieldName] as Guid?;
        }

        
    }
}