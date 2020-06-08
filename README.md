# Vipps Login

## Description

This repository contains the code to use Vipps Log In API by using OpenIdConnect in your ASP.NET application. Information about the Vipps Log In API can be found here: https://github.com/vippsas/vipps-login-api

This repository consists of two NuGet packages:

- Vipps.Login - OWIN Middleware that enables an application to use OpenIdConnect for authentication.
- Vipps.Login.Episerver - Episerver specific code for Vipps Login

## Features

- OWIN Middleware to support Vipps Login OpenIdConnect
- Library to simplify configuration and set up

## How to get started?

Start by installing the NuGet packages:

For the OWIN middleware

- `Install-Package Vipps.Login`

And for the Episerver extensions

- `Install-Package Vipps.Login.Episerver`

### Get API keys for Vipps Log In API

Get credentials from Vipps:
https://github.com/vippsas/vipps-developers/blob/master/vipps-getting-started.md#getting-the-api-keys
And configure them in your web config:
```
<add key="VippsLogin:ClientId" value="..." />
<add key="VippsLogin:ClientSecret" value="..." />
// Use the test url
<add key="VippsLogin:Authority" value="https://apitest.vipps.no/access-management-1.0/access" />
// Or use the production url
<add key="VippsLogin:Authority" value="https://api.vipps.no/access-management-1.0/access" />
```

### Prepare Episerver for OpenID Connect

Described in detail here: https://world.episerver.com/documentation/developer-guides/CMS/security/integrate-azure-ad-using-openid-connect/

#### 1. Disable Role and Membership Providers

```
<authentication mode="None" />
<membership>
  <providers>
    <clear/>
  </providers>
</membership>
<roleManager enabled="false">
  <providers>
    <clear/>
  </providers>
</roleManager>
```

#### 2. Configure Episerver to support claims

```
<episerver.framework>
  <securityEntity>
    <providers>
      <add name="SynchronizingProvider"
           type="EPiServer.Security.SynchronizingRolesSecurityEntityProvider, EPiServer"/>
    </providers>
  </securityEntity>
  <virtualRoles addClaims="true">
     //existing virtual roles
  </virtualRoles>
```

#### 3. Configure Vipps during app Startup

```csharp
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        // Enable the application to use a cookie to store information for the signed in user
        app.UseCookieAuthentication(new CookieAuthenticationOptions
        {
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            LoginPath = new PathString("/user")
        });

        // This should match CookieAuthentication AuthenticationType
        app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ApplicationCookie);
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
        // Trigger Vipps middleware on this path to start authentication
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
```

When the user goes to the path `https://your-site/vipps-login`, the Vipps middleware will be triggered and it will redirect the user to the Vipps log in environment. You will have to configure this redirect URL in Vipps, as described here: https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api-faq.md#how-can-i-activate-and-set-up-vipps-login
You can add a ReturnUrl to redirect the user once they are logged in, for example `https://your-site/vipps-login?ReturnUrl=/vipps-landing`.

Vipps is using the OpenIdConnect Authorization Code Grant flow, this means the user is redirected back to your environment with a Authorization token. The middleware will validate the token and exchange it for an `id_token` and an `access_token`. A `ClaimsIdentity` will be created which will contain the information of the scopes that you configured (email, name, addresses etc).

The Vipps UserInfo can be accessed by calling `IVippsLoginService.GetVippsUserInfo(ClaimsIdentity identity)`, this will give you the user info that was retrieved when the user logged in (cached).
