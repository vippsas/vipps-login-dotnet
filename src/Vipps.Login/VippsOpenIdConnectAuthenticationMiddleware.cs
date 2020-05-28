using Microsoft.Owin;
using Microsoft.Owin.Logging;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace Vipps.Login
{
    public class VippsOpenIdConnectAuthenticationMiddleware : OpenIdConnectAuthenticationMiddleware
    {
        private readonly ILogger _logger;

        public VippsOpenIdConnectAuthenticationMiddleware(
            OwinMiddleware next,
            IAppBuilder app,
            OpenIdConnectAuthenticationOptions options) : base(next, app, options)
        {
            _logger = app.CreateLogger<VippsOpenIdConnectAuthenticationMiddleware>();
        }

        protected override AuthenticationHandler<OpenIdConnectAuthenticationOptions> CreateHandler()
        {
            return new VippsOpenIdConnectAuthenticationHandler(_logger);
        }
    }
}