using System;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Vipps.Login
{
    public static class VippsOpenIdConnectAuthenticationExtensions
    {
        /// <summary>
        /// Adds the <see cref="OpenIdConnectAuthenticationMiddleware"/> into the OWIN runtime.
        /// </summary>
        /// <param name="app">The <see cref="IAppBuilder"/> passed to the configuration method</param>
        /// <param name="clientId">The application identifier.</param>
        /// <param name="metadataAddress">The discovery endpoint for obtaining metadata.</param>
        /// <returns>The updated <see cref="IAppBuilder"/></returns>
        public static IAppBuilder UseVippsOpenIdConnectAuthentication(this IAppBuilder app, string clientId, string metadataAddress)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }
            if (string.IsNullOrEmpty(clientId))
            {
                throw new ArgumentNullException(nameof(clientId));
            }
            if (string.IsNullOrEmpty(metadataAddress))
            {
                throw new ArgumentNullException(nameof(metadataAddress));
            }

            return app.UseVippsOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions()
            {
                ClientId = clientId,
                MetadataAddress = metadataAddress,
            });
        }

        /// <summary>
        /// Adds the <see cref="OpenIdConnectAuthenticationMiddleware"/> into the OWIN runtime.
        /// </summary>
        /// <param name="app">The <see cref="IAppBuilder"/> passed to the configuration method</param>
        /// <param name="openIdConnectOptions">A <see cref="OpenIdConnectAuthenticationOptions"/> contains settings for obtaining identities using the OpenIdConnect protocol.</param>
        /// <returns>The updated <see cref="IAppBuilder"/></returns>
        public static IAppBuilder UseVippsOpenIdConnectAuthentication(this IAppBuilder app, OpenIdConnectAuthenticationOptions openIdConnectOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (openIdConnectOptions == null)
            {
                throw new ArgumentNullException(nameof(openIdConnectOptions));
            }

            return app.Use(typeof(VippsOpenIdConnectAuthenticationMiddleware), app, openIdConnectOptions);
        }
    }
}