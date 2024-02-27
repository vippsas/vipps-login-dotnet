<!-- START_METADATA
---
title: Vipps Login for ASP.NET and Optimizely plugin
sidebar_position: 1
description: Allow customers to log in to Optimizely using the Vipps app.
pagination_next: null
pagination_prev: null
---
END_METADATA -->

# Vipps Login for ASP.NET and Optimizely

![Support and development by Geta ](./docs/images/geta.svg#gh-light-mode-only)![Support and development by Geta](./docs/images/geta_dark.svg#gh-dark-mode-only)

![Vipps](./docs/images/vipps.png) *Available for Vipps.*

![MobilePay](./docs/images/mp.png) *Not available for MobilePay.*


*This plugin is built and maintained by [Geta](https://getadigital.com/?epslanguage=en) and is hosted on [GitHub](https://github.com/vippsas/vipps-login-dotnet).*

<!-- START_COMMENT -->
💥 Please use the plugin pages on [https://developer.vippsmobilepay.com](https://developer.vippsmobilepay.com/docs/plugins-ext/login-dotnet/). 💥
<!-- END_COMMENT -->

Please keep up-to-date with updates as soon as they happen.

## Description

This repository contains the code to use Vipps Log In OpenIdConnect (OIDC) Authentication middleware in your ASP.NET application using OWIN.

This repository consists of three NuGet packages:

- `Vipps.Login` - OWIN Middleware that enables an application to use OpenIdConnect for authentication.
- `Vipps.Login.Episerver` - Episerver code for Vipps Login.
- `Vipps.Login.Episerver.Commerce` - Episerver Commerce code for Vipps Login.

For more information, about login, see the [Login API guide](https://developer.vippsmobilepay.com/docs/APIs/login-api/).

Note that Optimizely was previously called *Episerver*.

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

Activate and set up [Vipps Login](https://developer.vippsmobilepay.com/docs/developer-resources/portal/#how-to-set-up-login-for-your-sales-unit).

Configure a redirect URI to your site(s): `https://{your-site}/vipps-login`. Replace `{your-site}` with your own host name. It can be `localhost`, as well.

To use the `VippsLoginConfig` helper class, add the `ClientId` and the `ClientSecret` to the `Web.Config AppSettings`, as such:

```xml
<add key="VippsLogin:ClientId" value="..." />
<add key="VippsLogin:ClientSecret" value="..." />
<add key="VippsLogin:Authority" value="https://apitest.vipps.no/access-management-1.0/access" />
```

For production, use:

```xml
<add key="VippsLogin:Authority" value="https://api.vipps.no/access-management-1.0/access" />
```

See [Vipps test server](https://developer.vippsmobilepay.com/docs/test-environment/#test-server)
to find the default configuration for the Vipps OIDC middleware.

### Configuration

Now you can configure your ASP.NET or Episerver application:

- [ASP.NET application](docs/configure-asp-net.md)
- [Episerver application](docs/configure-episerver.md)

### Accessing Vipps user data

The [Vipps UserInfo](https://developer.vippsmobilepay.com/docs/APIs/userinfo-api/)
can be accessed by using the `GetVippsUserInfo(IIdentity identity)` method on `IVippsLoginService`.
This will give you the most recent user info that was retrieved when the user logged in (cached, stored as claims on the identity).

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

- [GitHub Repository](https://github.com/vippsas/vipps-login-dotnet)
- [Vipps Login API](https://developer.vippsmobilepay.com/docs/APIs/login-api/)
- [Vipps Developer Documentation](https://developer.vippsmobilepay.com/)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth)
- [OpenID Connect and Azure AD](https://docs.developers.optimizely.com/customized-commerce/docs/support-for-openid-connect-in-customized-commerce)

## Package maintainer

[Brian Weeteling](https://github.com/brianweet)

## Changelog

[Changelog](CHANGELOG.md)
