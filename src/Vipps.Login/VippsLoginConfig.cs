using System.Configuration;

namespace Vipps.Login
{
    public static class VippsLoginConfig
    {
        public static string ClientId => ConfigurationManager.AppSettings["VippsLogin:ClientId"] ?? string.Empty;
        public static string ClientSecret => ConfigurationManager.AppSettings["VippsLogin:ClientSecret"] ?? string.Empty;
        public static string Authority => ConfigurationManager.AppSettings["VippsLogin:Authority"] ?? string.Empty;
    }
}