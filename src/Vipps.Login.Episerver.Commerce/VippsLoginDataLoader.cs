using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Logging;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Business;
using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce
{
    public class VippsLoginDataLoader : IVippsLoginDataLoader
    {
        private readonly MapUserKey _mapUserKey;

        private static readonly ILogger Logger = LogManager.GetLogger(typeof(VippsLoginDataLoader));

        public VippsLoginDataLoader(MapUserKey mapUserKey)
        {
            _mapUserKey = mapUserKey;
        }

        public virtual IEnumerable<CustomerContact> FindContactsBySubjectGuid(Guid subjectGuid)
        {
            try
            {
                return BusinessManager
                    .List(ContactEntity.ClassName, new[]
                    {
                        new FilterElement(
                            MetadataConstants.VippsSubjectGuidFieldName,
                            FilterElementType.Equal,
                            subjectGuid
                        )
                    })
                    .OfType<CustomerContact>();
            }
            catch (Exception ex)
            {
                Logger.Error("Vipps.Login: could not load contacts by subjectGuid", ex);
                return Enumerable.Empty<CustomerContact>();
            }
        }

        public virtual IEnumerable<CustomerContact> FindContactsByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                Logger.Warning("Vipps.Login: could not load contacts for empty email");
                return Enumerable.Empty<CustomerContact>();
            }

            IEnumerable<CustomerContact> byEmail;
            try
            {
                byEmail = BusinessManager
                    .List(ContactEntity.ClassName, new[]
                    {
                        new FilterElement(
                            ContactEntity.FieldEmail,
                            FilterElementType.Equal,
                            email
                        )
                    }, new SortingElement[0], 0, 2)
                    .OfType<CustomerContact>();
            }
            catch (Exception ex)
            {
                Logger.Error("Vipps.Login: could not load contacts by email", ex);
                byEmail = Enumerable.Empty<CustomerContact>();
            }

            var byUserKey = CustomerContext.Current.GetContactByUserId(_mapUserKey.ToTypedString(email));
            if (byUserKey != null)
            {
                return new[] {byUserKey}
                    .Union(byEmail);
            }

            return byEmail;
        }

        public virtual IEnumerable<CustomerContact> FindContactsByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                Logger.Warning("Vipps.Login: could not load contacts for empty phone number");
                return Enumerable.Empty<CustomerContact>();
            }
            try
            {
                return BusinessManager
                    .List(AddressEntity.ClassName, new FilterElement[]
                    {
                        new OrBlockFilterElement(
                            new FilterElement(
                                AddressEntity.FieldDaytimePhoneNumber,
                                FilterElementType.Equal,
                                phone
                            ),
                            new FilterElement(
                                AddressEntity.FieldEveningPhoneNumber,
                                FilterElementType.Equal,
                                phone
                            ))
                    }, new SortingElement[0], 0, 50)
                    .OfType<CustomerAddress>()
                    .Where(x => x.ContactId.HasValue)
                    .Select(x => x.ContactId.Value)
                    .GroupBy(x => x)
                    .Select(x => BusinessManager.Load(ContactEntity.ClassName, x.First()))
                    .OfType<CustomerContact>();
            }
            catch (Exception ex)
            {
                Logger.Error("Vipps.Login: could not load contacts by phone number", ex);
                return Enumerable.Empty<CustomerContact>();
            }
        }
    }
}