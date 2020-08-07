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

        public static void SetVippsLinkAccountToken(this CustomerContact contact, Guid? accountTokenGuid)
        {
            contact[MetadataConstants.VippsLinkAccountTokenFieldName] = accountTokenGuid;
        }

        public static Guid? GetVippsLinkAccountToken(this CustomerContact contact)
        {
            return contact[MetadataConstants.VippsLinkAccountTokenFieldName] as Guid?;
        }
    }
}