# Vipps Log In for ASP.NET and Episerver

**Please keep up to date with updates as soon as they happen.**

**We encourage you to create an issue here if you require assistance or run in to a problem.**

## Description

This repository contains the code to use Vipps Log In OpenIdConnect (OIDC) Authentication middleware in your ASP.NET application using OWIN. Information about the Vipps Log In API can be found here: https://github.com/vippsas/vipps-login-api

This repository consists of three NuGet packages:

- Vipps.Login - OWIN Middleware that enables an application to use OpenIdConnect for authentication.
- Vipps.Login.Episerver - Episerver code for Vipps Login
- Vipps.Login.Episerver.Commerce - Episerver Commerce code for Vipps Login

## Features

- OWIN Middleware to support Vipps Login through OpenIdConnect
- Library to simplify Episerver configuration and set up

## How to get started?

Start by installing the NuGet packages:

For the OWIN middleware

- `Install-Package Vipps.Login`

And for the Episerver extensions

- `Install-Package Vipps.Login.Episerver`
- `Install-Package Vipps.Login.Episerver.Commerce`

### Get API keys for Vipps Log In API

Activate and set up Vipps Login: https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api-faq.md#how-can-i-activate-and-set-up-vipps-login

Configure a redirect URI to your site(s): `https://{your-site}/vipps-login` (replace `{your-site}` with your own host name, it can be localhost as well)

To use the `VippsLoginConfig` helper class, add the ClientId and the ClientSecret to the Web.Config AppSettings, as such:

```config
<add key="VippsLogin:ClientId" value="..." />
<add key="VippsLogin:ClientSecret" value="..." />
<add key="VippsLogin:Authority" value="https://apitest.vipps.no/access-management-1.0/access" />
```

For production use

```Here you can find the default configuration for the Vipps OIDC middleware.
<add key="VippsLogin:Authority" value="https://api.vipps.no/access-management-1.0/access" />
```

See https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api.md#base-urls

### Configuration

Now you can configure your ASP.NET or Episerver application:

- [ASP.NET application](docs/configure-asp-net.md)
- [Episerver application](docs/configure-episerver.md)

### Accessing Vipps user data

The Vipps UserInfo can be accessed by using the `GetVippsUserInfo(IIdentity identity)` method on `IVippsLoginService`, this will give you the most recent user info that was retrieved when the user logged in (cached, stored as claims on the identity).

```csharp
public class AccountController : Controller
{
    private readonly IVippsLoginService _vippsLoginService;
    public AccountController(IVippsLoginService vippsLoginService)
    {
        _vippsLoginService = vippsLoginService;
    }

    public ActionResult Index()
    {
        var userInfo =  _vippsLoginService.GetVippsUserInfo(User.Identity)
        ...
    }
}
```

## More info

- https://github.com/vippsas/vipps-login-api
- https://github.com/vippsas/vipps-developers
- https://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth
- https://world.episerver.com/documentation/developer-guides/commerce/security/support-for-openid-connect-in-episerver-commerce/

## Package maintainer

https://github.com/brianweet

## Changelog

[Changelog](CHANGELOG.md)
