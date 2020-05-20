using System.Configuration;

namespace Epi.VippsLogin
{
    public static class VippsLoginConfig
    {
        public static string ClientId => ConfigurationManager.AppSettings["VippsLogin:ClientId"];
        public static string ClientSecret => ConfigurationManager.AppSettings["VippsLogin:ClientSecret"];
        public static string Authority => ConfigurationManager.AppSettings["VippsLogin:Authority"];
    }
}