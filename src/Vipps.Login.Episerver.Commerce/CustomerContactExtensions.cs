using System;
using Mediachase.Commerce.Customers;
using Vipps.Login.Models;

namespace Vipps.Login.Episerver.Commerce
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

        public static void MapVippsUserInfo(
            this CustomerContact contact,
            VippsUserInfo userInfo
        )
        {
            if (userInfo == null) throw new ArgumentNullException(nameof(userInfo));
            contact.Email = userInfo.Email;
            contact.FirstName = userInfo.GivenName;
            contact.LastName = userInfo.FamilyName;
            contact.FullName = userInfo.Name;
            contact.BirthDate = userInfo.BirthDate;
            contact.SetVippsSubject(userInfo.Sub);
        }
    }
}