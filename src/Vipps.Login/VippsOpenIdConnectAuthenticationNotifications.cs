using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Vipps.Login
{
    public class VippsOpenIdConnectAuthenticationNotifications : OpenIdConnectAuthenticationNotifications
    {
        public VippsOpenIdConnectAuthenticationNotifications()
        {
            RedirectToIdentityProvider = DefaultRedirectToIdentityProvider;
            AuthorizationCodeReceived = DefaultAuthorizationCodeReceived;
            AuthenticationFailed = DefaultAuthenticationFailed;
        }

        protected virtual Task DefaultRedirectToIdentityProvider(RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            // In order to support multi site we change the return uri based on the current request
            // For example https://your-first-site/vipps-login or https://your-second-site/vipps-login 
            context.ProtocolMessage.RedirectUri = GetMultiSiteRedirectUri(context.ProtocolMessage.RedirectUri, context.Request);

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
            if (context.OwinContext.Response.StatusCode == 401 &&
                VippsHelpers.IsXhrRequest(context.OwinContext.Request))
            {
                context.HandleResponse();
            }

            return Task.FromResult(0);
        }

        protected virtual async Task DefaultAuthorizationCodeReceived(AuthorizationCodeReceivedNotification notification)
        {
            var configuration =
                await notification.Options.ConfigurationManager
                    .GetConfigurationAsync(notification.Request.CallCancelled)
                    .ConfigureAwait(false);

            var tokenClient = new TokenClient(
                () => new HttpClient(),
                new TokenClientOptions
                {
                    Address = configuration.TokenEndpoint,
                    ClientId = notification.Options.ClientId,
                    ClientSecret = notification.Options.ClientSecret,
                    ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader
                }
            );
            var url = GetMultiSiteRedirectUri(notification.RedirectUri, notification.Request);
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
                    $"Failed to parse token response body as JSON. Status Code: {(int)tokenResponse.HttpStatusCode}. ",
                    ex);
            }

            if (tokenResponse.IsError)
            {
                throw new OpenIdConnectProtocolException(message.ErrorDescription);
            }

            notification.TokenEndpointResponse = message;
        }

        private Task DefaultAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> context)
        {
            // Here we just display the error message
            context.HandleResponse();
            context.Response.Write(context.Exception.Message);
            return Task.FromResult(0);
        }

        protected virtual string GetMultiSiteRedirectUri(string currentRedirectUri, IOwinRequest request)
        {
            return VippsHelpers.GetMultiSiteRedirectUri(currentRedirectUri, request);
        }
    }
}