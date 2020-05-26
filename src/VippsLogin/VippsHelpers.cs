using System;
using System.Configuration;
using Microsoft.Owin;
using Owin;

namespace Epi.VippsLogin
{
    public class VippsHelpers
    {
        /// <summary>
        /// Get the redirect uri based on the owin request
        /// </summary>
        /// <param name="currentRedirectUri">The <see cref="IAppBuilder"/> passed to the configuration method</param>
        /// <param name="owinRequest">The current request.</param>
        /// <returns>The updated redirect uri to use in the auth flow</returns>
        public static string GetMultiSiteRedirectUri(string currentRedirectUri, IOwinRequest owinRequest)
        {
            if (!string.IsNullOrWhiteSpace(currentRedirectUri) &&
                currentRedirectUri.IndexOf("logout.aspx", StringComparison.OrdinalIgnoreCase) <= -1)
            {
                return currentRedirectUri;
            }

            if (!owinRequest.Uri.Scheme.Equals("https"))
            {
                throw new ConfigurationErrorsException(
                    "Request scheme is invalid, please use HTTPS. " +
                    "Use OpenIdConnectAuthenticationOptions.OverrideLoginReturnUrl or " +
                    "try to set up your url in admin mode/manage websites correctly");
            }

            // Use left part of request uri (strips query)
            return owinRequest.Uri.GetLeftPart(UriPartial.Path);
        }

        public static bool IsXhrRequest(IOwinRequest request)
        {
            const string xRequestedWith = "X-Requested-With";

            var query = request.Query;
            if (query != null && query[xRequestedWith] == "XMLHttpRequest")
            {
                return true;
            }

            var headers = request.Headers;
            return headers != null && headers[xRequestedWith] == "XMLHttpRequest";
        }
    }
}