using System.Security.Claims;
using Vipps.Login.Models;

namespace Vipps.Login
{
    public interface IVippsLoginService
    {
        VippsUserInfo GetVippsUserInfo(ClaimsIdentity identity);
        bool IsVippsIdentity(ClaimsIdentity identity);
    }
}