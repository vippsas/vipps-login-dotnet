using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Vipps.Login
{
    public class VippsOpenIdConnectAuthenticationOptions : OpenIdConnectAuthenticationOptions
    {
        public VippsOpenIdConnectAuthenticationOptions()
        {
            AuthenticationType = VippsAuthenticationDefaults.AuthenticationType;
            ResponseType = OpenIdConnectResponseType.Code;
            ResponseMode = OpenIdConnectResponseMode.Query;
            RedeemCode = true;
            TokenValidationParameters = new TokenValidationParameters
            {
                RoleClaimType = ClaimTypes.Role
            };
        }
    }
}