using IdentityModel.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Microsoft.Owin.Extensions;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Epi.VippsLogin
{
    public static class AppBuilderExtension
    {
        public static void ConfigureAuthentication(this IAppBuilder app)
        {
            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions
            {
                ClientId = VippsLoginConfig.ClientId,
                ClientSecret = VippsLoginConfig.ClientSecret,
                Authority = VippsLoginConfig.Authority,
                Scope = string.Join(" ", new[]
                {
                    OpenIdConnectScope.OpenId,
                    OpenIdConnectScope.Name,
                    OpenIdConnectScope.Email,
                    OpenIdConnectScope.Address,
                    OpenIdConnectScope.PhoneNumber,
                    OpenIdConnectScope.BirthDate
                }),
                ResponseType = OpenIdConnectResponseType.Code,
                ResponseMode = OpenIdConnectResponseMode.Query,
                AuthenticationMode = AuthenticationMode.Passive,
                SaveTokens = true,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    RoleClaimType = ClaimTypes.Role
                },
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    RedirectToIdentityProvider = context =>
                    {
                        // Here you can change the return uri based on multisite
                        context.ProtocolMessage.RedirectUri = GetMultiSiteRedirectUri(context.ProtocolMessage, context.Request);

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
                        if (context.OwinContext.Response.StatusCode == 401 && IsXhrRequest(context.OwinContext.Request))
                        {
                            context.HandleResponse();
                        }
                        return Task.FromResult(0);
                    },
                    AuthorizationCodeReceived = async notification =>
                    {
                        // https://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth
                        // Exchange Authorization Code for an ID Token and an Access Token
                        notification.TokenEndpointResponse = await GetTokenEndpointResponse(notification);
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
                    },
                }
            });
            app.UseStageMarker(PipelineStage.Authenticate);
            app.Map("/vipps-login", map => map.Run(ctx =>
            {
                if (ctx.Authentication.User?.Identity == null || !ctx.Authentication.User.Identity.IsAuthenticated)
                {
                    ctx.Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                    return Task.Delay(0);
                }

                var returnUrl = ctx.Request.Query.Get("ReturnUrl") ?? "/";

                return Task.Run(() => ctx.Response.Redirect(returnUrl));
            }));
            app.Map("/logout", map =>
            {
                map.Run(context =>
                {
                    context.Authentication.SignOut();
                    return Task.FromResult(0);
                });
            });
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
        }

        public static async Task<OpenIdConnectMessage> GetTokenEndpointResponse(AuthorizationCodeReceivedNotification notification)
        {
            var configuration =
                await notification.Options.ConfigurationManager
                    .GetConfigurationAsync(notification.Request.CallCancelled)
                    .ConfigureAwait(false);

            var tokenClient = new TokenClient(
                () => notification.Options.Backchannel,
                new TokenClientOptions
                {
                    Address = configuration.TokenEndpoint,
                    ClientId = notification.Options.ClientId,
                    ClientSecret = notification.Options.ClientSecret,
                    ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader
                }
            );
            var url =
                GetMultiSiteRedirectUri(notification.ProtocolMessage, notification.Request);
            var tokenResponse = await tokenClient.RequestAuthorizationCodeTokenAsync(
                    notification.Code,
                    url)
                .ConfigureAwait(false);

            // Error handling:
            // 1. If the response body can't be parsed as json, throws.
            // 2. If the response's status code is not in 2XX range, throw OpenIdConnectProtocolException. If the body is correct parsed,
            //    pass the error information from body to the exception.
            OpenIdConnectMessage message;
            try
            {
                message = new OpenIdConnectMessage(tokenResponse.Json);
            }
            catch (Exception ex)
            {
                throw new OpenIdConnectProtocolException(
                    $"Failed to parse token response body as JSON. Status Code: {(int) tokenResponse.HttpStatusCode}. ", ex);
            }

            if (tokenResponse.IsError)
            {
                throw new OpenIdConnectProtocolException(message.ErrorDescription);
            }

            return message;
        }

        private static string GetMultiSiteRedirectUri(OpenIdConnectMessage protocolMessage, IOwinRequest owinRequest)
        {
            if (protocolMessage.RedirectUri != null &&
                protocolMessage.RedirectUri.IndexOf("logout.aspx", StringComparison.OrdinalIgnoreCase) <= -1)
            {
                return protocolMessage.RedirectUri;
            }

            if (!owinRequest.Uri.Scheme.Equals("https"))
            {
                throw new ConfigurationErrorsException(
                    "SiteUrl scheme is invalid, please use HTTPS. " +
                    "Use OpenIdConnectAuthenticationOptions.OverrideLoginReturnUrl or " +
                    "try to set up your url in admin mode/manage websites correctly");
            }

            // Use request uri as return uri to support multi-site environments
            var redirectUrl = new UriBuilder(
                owinRequest.Uri.Scheme,
                owinRequest.Uri.Host,
                owinRequest.Uri.Port,
                HttpContext.Current.Request.Url.AbsolutePath)
                .ToString()
                .Replace(":443", string.Empty);

            return redirectUrl;

        }

        private static bool IsXhrRequest(IOwinRequest request)
        {
            const string xRequestedWith = "X-Requested-With";

            var query = request.Query;
            if ((query != null) && (query[xRequestedWith] == "XMLHttpRequest"))
            {
                return true;
            }

            var headers = request.Headers;
            return (headers != null) && (headers[xRequestedWith] == "XMLHttpRequest");
        }
    }
}