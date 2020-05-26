using System;
using System.Threading.Tasks;
using IdentityModel.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Vipps.Login
{
    public class VippsOpenIdConnectAuthenticationHandler : OpenIdConnectAuthenticationHandler
    {
        public VippsOpenIdConnectAuthenticationHandler(ILogger logger) : base(logger)
        {
        }

        protected override async Task<OpenIdConnectMessage> RedeemAuthorizationCodeAsync(OpenIdConnectMessage tokenEndpointRequest)
        {
            var configuration =
                await Options.ConfigurationManager.GetConfigurationAsync(Context.Request.CallCancelled)
                    .ConfigureAwait(false);

            var tokenClient = new TokenClient(
                () => Backchannel,
                new TokenClientOptions
                {
                    Address = configuration.TokenEndpoint,
                    ClientId = Options.ClientId,
                    ClientSecret = Options.ClientSecret,
                    ClientCredentialStyle = ClientCredentialStyle.AuthorizationHeader
                }
            );
            var url =
                VippsHelpers.GetMultiSiteRedirectUri(tokenEndpointRequest.RedirectUri, Context.Request);
            var tokenResponse = await tokenClient.RequestAuthorizationCodeTokenAsync(
                    tokenEndpointRequest.Code,
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
                    $"Failed to parse token response body as JSON. Status Code: {(int)tokenResponse.HttpStatusCode}. ", ex);
            }

            if (tokenResponse.IsError)
            {
                throw new OpenIdConnectProtocolException(message.ErrorDescription);
            }

            return message;
        }
    }
}