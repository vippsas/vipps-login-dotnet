using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Vipps.Login.Episerver
{
    /// <summary>Options class to facilitate easy configuration of Vipps authentication.</summary>
    public class VippsInitAuthenticationOptions
    {
        /// <summary>Should match the value on configured cookie authentication.</summary>
        public string CookieAuthType { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Authority { get; set; }
        /// <summary>Identifiers used to specify what access privileges are being requested see <see cref="VippsScopes"/>.</summary>
        public string[] Scopes { get; set; }
        // Store the id_token and access_token on the ClaimsIdentity
        public bool SaveTokens { get; set; }
        /// <summary>
        /// Function which will be called after creating the identity and its roles but before syncing to the db.
        /// Can be used to add/delete claims or change other properties
        /// </summary>
        public Func<ClaimsIdentity, Task> SecurityTokenValidated { get; set; } = notification => Task.FromResult(0);
        /// <summary>Claim that will be used as username, default value is ClaimTypes.Email</summary>
        public string UserNameClaim { get; set; } = ClaimTypes.Email;
        /// <summary>Gets or sets the login path, default value is "/vipps-login".</summary>
        public string LoginPath { get; set; } = "/vipps-login";
        /// <summary>Gets or sets the logout path, default value is "/vipps-login".</summary>
        public string LogoutPath { get; set; } = "/vipps-logout";
    }
}