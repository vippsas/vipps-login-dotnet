using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Helpers;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using IdentityModel.Client;
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
        public static void ConfigureAuthentication(this IAppBuilder app)
        {
            app.UseVippsOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = VippsAuthenticationDefaults.AuthenticationType,
                ClientId = VippsLoginConfig.ClientId,
                ClientSecret = VippsLoginConfig.ClientSecret,
                Authority = VippsLoginConfig.Authority,
                Scope = string.Join(" ", 
                    VippsScopes.OpenId,
                    VippsScopes.Name,
                    VippsScopes.Email,
                    VippsScopes.Address,
                    VippsScopes.PhoneNumber,
                    VippsScopes.BirthDate),
                ResponseType = OpenIdConnectResponseType.Code,
                ResponseMode = OpenIdConnectResponseMode.Query,
                RedeemCode = true,
                AuthenticationMode = AuthenticationMode.Passive,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    RoleClaimType = ClaimTypes.Role
                },
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

                        //XHR requests cannot handle redirects to a login screen, return 401
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
                            new Claim(identity.NameClaimType, identity.FindFirst(ClaimTypes.Email)?.Value)
                        );

                        var configuration =
                            await ctx.Options.ConfigurationManager
                                .GetConfigurationAsync(ctx.Request.CallCancelled)
                                .ConfigureAwait(false);
                        var response = await new HttpClient().GetUserInfoAsync(new UserInfoRequest
                        {
                            Address = configuration.UserInfoEndpoint,
                            Token = ctx.ProtocolMessage.AccessToken
                        });
                        if (response.IsError)
                            throw new Exception(response.Error);

                        // Set roles
                        identity.AddClaim(new Claim(ClaimTypes.Role, EpiApplicationRoles.Administrators,
                            ClaimValueTypes.String));
                        identity.AddClaim(new Claim(ClaimTypes.Role, EpiApplicationRoles.WebAdmins,
                            ClaimValueTypes.String));

                        // Sync user and the roles to Epi
                        await ServiceLocator.Current.GetInstance<ISynchronizingUserService>()
                            .SynchronizeAsync(identity, new List<string>());
                    },
                    AuthenticationFailed = context =>
                    {
                        context.HandleResponse();
                        context.Response.Write(context.Exception.Message);
                        return Task.FromResult(0);
                    }
                }
            });
            app.UseStageMarker(PipelineStage.Authenticate);
            app.Map("/vipps-login", map => map.Run(ctx =>
            {
                if (ctx.Authentication.User?.Identity == null || !ctx.Authentication.User.Identity.IsAuthenticated)
                {
                    ctx.Authentication.Challenge(VippsAuthenticationDefaults.AuthenticationType);
                    return Task.Delay(0);
                }

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
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
        }
    }
}