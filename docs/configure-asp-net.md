# Configure Vipps Log In for ASP.NET

Here you can find the default configuration for the Vipps OIDC middleware. Some tips:

1. Be sure to configure only the scopes you actually need.
2. If authentication fails, we suggest redirecting to the normal login page and show an informational message.

To trigger authentication middleware, just add a link to `/vipps-login` or redirect the user to `/vipps-login`.

```csharp
[assembly: OwinStartup(typeof(Your.Namespace.Startup))]
namespace Your.Namespace
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed in user
            // and to use a cookie to temporarily store information about a user logging in with a third party login provider
            // Configure the sign in cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = CookieAuthenticationDefaults.AuthenticationType,
                LoginPath = new PathString("/vipps-login")
            });

            ConfigureVippsAuth(app);
        }

        private void ConfigureVippsAuth(IAppBuilder app)
        {
            // This should match CookieAuthentication AuthenticationType above ^
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            // Vipps OIDC configuration starts here
            app.UseOpenIdConnectAuthentication(
                new VippsOpenIdConnectAuthenticationOptions(
                    VippsLoginConfig.ClientId,
                    VippsLoginConfig.ClientSecret,
                    VippsLoginConfig.Authority)
            {
                // 1. Here you pass in the scopes you need
                Scope = string.Join(" ", new[]
                {
                    VippsScopes.ApiV2,
                    VippsScopes.OpenId,
                    VippsScopes.Email,
                    VippsScopes.Name,
                    VippsScopes.Address,
                    VippsScopes.PhoneNumber,
                    VippsScopes.BirthDate
                }),
                // Passive means it will only trigger if you explicitly ask it to
                // See the /vipps-login path below
                AuthenticationMode = AuthenticationMode.Passive,
                // Various notifications that we can handle during the auth flow
                // You might want to override:
                // RedirectToIdentityProvider - Redirecting to Vipps using correct RedirectUri
                // AuthenticationFailed - Handle exceptions (otherwise it writes exception to response)
                Notifications = new VippsOpenIdConnectAuthenticationNotifications
                {
                    // This will be called after creating the identity and its roles
                    // Can be used to add/delete claims or change other properties or store information in the db
                    SecurityTokenValidated = async context =>
                    {
                        // Prevent redirecting to external Uris
                        var redirectUri = new Uri(context.AuthenticationTicket.Properties.RedirectUri,
                            UriKind.RelativeOrAbsolute);
                        if (redirectUri.IsAbsoluteUri)
                        {
                            context.AuthenticationTicket.Properties.RedirectUri = redirectUri.PathAndQuery;
                        }

                        var configuration =
                            await context.Options.ConfigurationManager
                                .GetConfigurationAsync(context.Request.CallCancelled)
                                .ConfigureAwait(false);
                        var service = new VippsLoginService();

                        // Use access token to retrieve claims from UserInfo endpoint
                        var claims = await service.GetUserInfoClaims(
                            configuration.UserInfoEndpoint,
                            context.ProtocolMessage.AccessToken);
                        // Add claims to identity
                        context.AuthenticationTicket.Identity.AddClaims(claims);
                        // Get UserInfo from identity
                        var userInfo = service.GetVippsUserInfo(context.AuthenticationTicket.Identity);

                        // By default we use email address as username
                        var identity = context.AuthenticationTicket.Identity;
                        identity.AddClaim(
                            new Claim(identity.NameClaimType, userInfo.Email)
                        );

                        // TODO: Store user in db?
                    },
                    AuthenticationFailed = context =>
                    {
                        // 2. Redirect to login or error page and display message
                        // See context.Exception for more details
                        var message = "Something went wrong. Please contact customer support.";
                        context.HandleResponse();
                        context.Response.Redirect($"/login?error={message}");
                        return Task.FromResult(0);
                    }
                }
            });
            app.UseStageMarker(PipelineStage.Authenticate);
            // Trigger Vipps middleware on this path to start authentication
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
                    context.Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
                    return Task.FromResult(0);
                });
            });
            // Required for AntiForgery to work
            // Otherwise it'll throw an exception about missing claims
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
        }
    }
}
```
