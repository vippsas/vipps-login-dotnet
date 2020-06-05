using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.Owin.Security;
using Owin;

namespace Vipps.Login.Episerver
{
    public static class AppBuilderExtensions
    {
        public static void ConfigureVippsAuthentication(this IAppBuilder app)
        {
            // This should match CookieAuthentication AuthenticationType
            // Default is CookieAuthenticationDefaults.AuthenticationType
            app.SetDefaultSignInAsAuthenticationType("Cookies");
            app.UseOpenIdConnectAuthentication(new VippsOpenIdConnectAuthenticationOptions
            {
                // Your credentials
                ClientId = VippsLoginConfig.ClientId,
                ClientSecret = VippsLoginConfig.ClientSecret,
                Authority = VippsLoginConfig.Authority,
                // Here you pass in the scopes you need
                Scope = string.Join(" ", new []
                {
                    VippsScopes.OpenId,
                    VippsScopes.Email,
                    VippsScopes.Name,
                    VippsScopes.BirthDate,
                    VippsScopes.Address,
                    VippsScopes.PhoneNumber
                }),
                // Various notifications that we can handle during the auth flow
                // By default it will handle:
                // RedirectToIdentityProvider - Redirecting to Vipps using correct RedirectUri
                // AuthorizationCodeReceived - Exchange Authentication code for id_token and access_token
                // DefaultAuthenticationFailed- Display error message on failed auth
                Notifications = new VippsOpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenValidated = async ctx =>
                    {
                        // Prevent redirecting to external Uris
                        var redirectUri = new Uri(ctx.AuthenticationTicket.Properties.RedirectUri,
                            UriKind.RelativeOrAbsolute);
                        if (redirectUri.IsAbsoluteUri)
                        {
                            ctx.AuthenticationTicket.Properties.RedirectUri = redirectUri.PathAndQuery;
                        }

                        // By default we use email address as username
                        var identity = ctx.AuthenticationTicket.Identity;
                        identity.AddClaim(
                            new Claim(identity.NameClaimType, identity.FindFirst(ClaimTypes.Email)?.Value)
                        );
                        // Here you can add extra roles to the user
                        // For example to allow CMS access:
                        identity.AddClaim(new Claim(ClaimTypes.Role, EpiApplicationRoles.CmsEditors));

                        // Sync user and the roles to Epi
                        await ServiceLocator.Current.GetInstance<ISynchronizingUserService>()
                            .SynchronizeAsync(identity, new List<string>());
                    }
                }
            });
            // Trigger Vipps middleware to start authentication
            app.Map("/vipps-login", map => map.Run(ctx =>
            {
                if (ctx.Authentication.User?.Identity == null || !ctx.Authentication.User.Identity.IsAuthenticated)
                {
                    ctx.Authentication.Challenge(VippsAuthenticationDefaults.AuthenticationType);
                    return Task.Delay(0);
                }
                // Return to this url after authenticating
                var returnUrl = ctx.Request.Query.Get("ReturnUrl") ?? "/";

                return Task.Run(() => ctx.Response.Redirect(returnUrl));
            }));
            app.Map("/vipps-logout", map =>
            {
                map.Run(context =>
                {
                    context.Authentication.SignOut(VippsAuthenticationDefaults.AuthenticationType);
                    return Task.FromResult(0);
                });
            });
        }
    }
}