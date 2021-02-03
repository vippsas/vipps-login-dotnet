using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Vipps.Login.Models;

namespace Vipps.Login
{
    public interface IVippsLoginService
    {
        bool IsVippsIdentity(IIdentity identity);
        bool IsVippsIdentity(ClaimsIdentity identity);
        VippsUserInfo GetVippsUserInfo(IIdentity identity);
        VippsUserInfo GetVippsUserInfo(ClaimsIdentity identity);
        Task<IEnumerable<Claim>> GetUserInfoClaims(string userInfoEndpoint, string accessToken);
    }
}