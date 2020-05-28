using System.Security.Claims;
using System.Threading.Tasks;
using Vipps.Login.Models;

namespace Vipps.Login
{
    public interface IVippsLoginService
    {
        Task<VippsUserInfo> GetVippsUserInfo(string userInfoEndpoint, string accessToken);
        VippsUserInfo GetVippsUserInfo(ClaimsIdentity identity);
    }
}