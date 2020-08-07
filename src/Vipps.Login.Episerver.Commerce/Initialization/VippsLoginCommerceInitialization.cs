using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Vipps.Login.Episerver.Commerce.Initialization
{
    [InitializableModule]
    public class VippsLoginCommerceInitialization : IConfigurableModule
    {
        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ICustomerContactService, CustomerContactService>();
            context.Services.AddTransient<IVippsLoginCommerceService, VippsLoginCommerceService>();
            context.Services.AddTransient<IVippsLoginMapper, VippsLoginMapper>();
            context.Services.AddTransient<IVippsLoginDataLoader, VippsLoginDataLoader>();
            context.Services.AddTransient<IVippsLoginSanityCheck, VippsLoginSanityCheck>();
        }
    }
}