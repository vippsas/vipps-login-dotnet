using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using Mediachase.BusinessFoundation.Data;
using Mediachase.BusinessFoundation.Data.Meta.Management;
using Mediachase.Commerce.Customers;

namespace Vipps.Login.Episerver.Commerce.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Commerce.Initialization.InitializationModule))]
    internal class MetadataInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            CreateMetaField(AddressEntity.ClassName, MetadataConstants.VippsAddressTypeFieldName, MetadataConstants.VippsAddressTypeFriendlyName);
            CreateGuidMetaField(ContactEntity.ClassName, MetadataConstants.VippsSubjectGuidFieldName, MetadataConstants.VippsSubjectGuidFriendlyName);
            CreateGuidMetaField(ContactEntity.ClassName, MetadataConstants.VippsLinkAccountTokenFieldName, MetadataConstants.VippsLinkAccountTokenFriendlyName);
        }

        private void CreateMetaField(string metaClassName, string metaFieldName, string friendlyName, bool isNullable = true, int maxLength = 255, bool isUnique = false)
        {
            var metaClass = GetMetaClass(metaClassName, metaFieldName);
            if (metaClass == null)
            {
                return;
            }

            using (var metaFieldBuilder = new MetaFieldBuilder(metaClass))
            {
                metaFieldBuilder.MetaClass.AccessLevel = AccessLevel.Customization;
                metaFieldBuilder.CreateText(metaFieldName, friendlyName, isNullable, maxLength, isUnique);
                metaFieldBuilder.SaveChanges();
            }
        }

        private void CreateGuidMetaField(string metaClassName, string metaFieldName, string friendlyName, bool isNullable = true)
        {
            var metaClass = GetMetaClass(metaClassName, metaFieldName);
            if (metaClass == null)
            {
                return;
            }

            using (var metaFieldBuilder = new MetaFieldBuilder(metaClass))
            {
                metaFieldBuilder.MetaClass.AccessLevel = AccessLevel.Customization;
                metaFieldBuilder.CreateGuid(metaFieldName, friendlyName, isNullable);
                metaFieldBuilder.SaveChanges();
            }
        }

        private static MetaClass GetMetaClass(string metaClassName, string metaFieldName)
        {
            var metaClass = DataContext.Current.GetMetaClass(metaClassName);
            if (metaClass == null || metaClass.Fields[metaFieldName] != null)
            {
                return null;
            }

            return metaClass;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}