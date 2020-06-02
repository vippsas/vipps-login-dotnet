using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Vipps.Login.Episerver
{
    public static class AppBuilderExtensions
    {
        private const string ReturnUrl = "ReturnUrl";

        public static void ConfigureVippsAuthentication(this IAppBuilder app,
            VippsInitAuthenticationOptions options
            )
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Scopes == null) throw new ArgumentNullException(nameof(options.Scopes));
            if (!options.Scopes.Contains(VippsScopes.OpenId)) throw new ArgumentNullException(nameof(options.Scopes), "The VippsScopes.OpenId scope is mandatory");
            if (options.UserNameClaim == null) throw new ArgumentNullException(nameof(options.UserNameClaim));

            app.SetDefaultSignInAsAuthenticationType(options.CookieAuthType);
            app.UseVippsOpenIdConnectAuthentication(new VippsOpenIdConnectAuthenticationOptions
            {
                // This should match CookieAuthentication AuthenticationType
                AuthenticationType = VippsAuthenticationDefaults.AuthenticationType,
                // Your credentials
                ClientId = VippsLoginConfig.ClientId,
                ClientSecret = VippsLoginConfig.ClientSecret,
                Authority = VippsLoginConfig.Authority,
                // Here you pass in the scopes you need
                Scope = string.Join(" ", options.Scopes),
                // Store tokens on identity
                SaveTokens = options.SaveTokens,
                // Various notifications that we can handle during the auth flow
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = context =>
                    {
                        // In order to support multi site we change the return uri based on the current request
                        // For example https://your-first-site/vipps-login or https://your-second-site/vipps-login 
                        context.ProtocolMessage.RedirectUri =
                            VippsHelpers.GetMultiSiteRedirectUri(context.ProtocolMessage.RedirectUri, context.Request);

                        // To avoid a redirect loop to the federation server send 403
                        // when user is authenticated but does not have access
                        if (context.OwinContext.Response.StatusCode == 401 &&
                            context.OwinContext.Authentication.User?.Identity != null &&
                            context.OwinContext.Authentication.User.Identity.IsAuthenticated)
                        {
                            context.OwinContext.Response.StatusCode = 403;
                            context.HandleResponse();
                        }

                        // XHR requests cannot handle redirects to a login screen, return 401
                        if (context.OwinContext.Response.StatusCode == 401 && VippsHelpers.IsXhrRequest(context.OwinContext.Request))
                        {
                            context.HandleResponse();
                        }

                        return Task.FromResult(0);
                    },
                    SecurityTokenValidated = async ctx =>
                    {
                        // Prevent redirecting to external Uris
                        var redirectUri = new Uri(ctx.AuthenticationTicket.Properties.RedirectUri,
                            UriKind.RelativeOrAbsolute);
                        if (redirectUri.IsAbsoluteUri)
                        {
                            ctx.AuthenticationTicket.Properties.RedirectUri = redirectUri.PathAndQuery;
                        }

                        // Set username
                        var identity = ctx.AuthenticationTicket.Identity;
                        identity.AddClaim(
                            new Claim(identity.NameClaimType, identity.FindFirst(options.UserNameClaim)?.Value)
                        );

                        // Can be used to add/delete claims or change other properties
                        if (options.SecurityTokenValidated != null)
                        {
                            await options.SecurityTokenValidated(identity);
                        }

                        // Sync user and the roles to Epi
                        await ServiceLocator.Current.GetInstance<ISynchronizingUserService>()
                            .SynchronizeAsync(identity, new List<string>());
                    },
                    AuthenticationFailed = context =>
                    {
                        // Here you can decide what to do if authentication failed
                        context.HandleResponse();
                        context.Response.Write(context.Exception.Message);
                        return Task.FromResult(0);
                    }
                }
            });
            app.UseStageMarker(PipelineStage.Authenticate);

            if (!string.IsNullOrWhiteSpace(options.LoginPath))
            {
                app.Map(options.LoginPath, map => map.Run(ctx =>
                {
                    if (ctx.Authentication.User?.Identity == null || !ctx.Authentication.User.Identity.IsAuthenticated)
                    {
                        ctx.Authentication.Challenge(VippsAuthenticationDefaults.AuthenticationType);
                        return Task.Delay(0);
                    }

                    var returnUrl = ctx.Request.Query.Get(ReturnUrl) ?? "/";

                    return Task.Run(() => ctx.Response.Redirect(returnUrl));
                }));
            }

            if (!string.IsNullOrWhiteSpace(options.LogoutPath))
            {
                app.Map(options.LogoutPath, map =>
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
}