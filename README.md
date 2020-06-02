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

        // This will configure Vipps OpenIdConnect middleware for you
        app.ConfigureVippsAuthentication(new VippsInitAuthenticationOptions
        {
            // This should match CookieAuthentication AuthenticationType
            CookieAuthType = DefaultAuthenticationTypes.ApplicationCookie,
            // Your credentials
            ClientId = VippsLoginConfig.ClientId,
            ClientSecret = VippsLoginConfig.ClientSecret,
            Authority = VippsLoginConfig.Authority,
            // Here you pass in the scopes you need
            Scopes = new[]
            {
                VippsScopes.OpenId,
                VippsScopes.Email,
                VippsScopes.Name,
                VippsScopes.Address,
                VippsScopes.PhoneNumber,
                VippsScopes.BirthDate
            },
            SecurityTokenValidated = identity =>
            {
                // This which will be called after creating the identity and its roles but before syncing to the db.
                // Can be used to add/delete claims or change other properties
                // For example to add access to the CMS or to create a user account automatically
                return Task.FromResult(0);
            }
        });
    }
}
```

When the user goes to the path `https://your-site/vipps-login`, the Vipps middleware will be triggered and it will redirect the user to the Vipps log in environment. You will have to configure this redirect URL in Vipps, as described here: https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api-faq.md#how-can-i-activate-and-set-up-vipps-login
You can add a ReturnUrl to redirect the user once they are logged in, for example `https://your-site/vipps-login?ReturnUrl=/vipps-landing`.

Vipps is using the OpenIdConnect Authorization Code Grant flow, this means the user is redirected back to your environment with a Authorization token. The middleware will validate the token and exchange it for an `id_token` and an `access_token`. A `ClaimsIdentity` will be created which will contain the information of the scopes that you configured (email, name, addresses etc).

The Vipps UserInfo can be accessed by calling `IVippsLoginService.GetVippsUserInfo(ClaimsIdentity identity)`, this will give you the user info that was retrieved when the user logged in (cached).
