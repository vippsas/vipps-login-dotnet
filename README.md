# Vipps Log In OIDC Authentication middleware for ASP.NET and Episerver

## Description

This repository contains the code to use Vipps Log In OpenIdConnect (OIDC) Authentication middleware in your ASP.NET application using OWIN. Information about the Vipps Log In API can be found here: https://github.com/vippsas/vipps-login-api

This repository consists of three NuGet packages:

- Vipps.Login - OWIN Middleware that enables an application to use OpenIdConnect for authentication.
- Vipps.Login.Episerver - Episerver code for Vipps Login
- Vipps.Login.Episerver.Commerce - Episerver Commerce code for Vipps Login

## Features

- OWIN Middleware to support Vipps Login OpenIdConnect
- Library to simplify configuration and set up

## How to get started?

Start by installing the NuGet packages:

For the OWIN middleware

- `Install-Package Vipps.Login`

And for the Episerver extensions

- `Install-Package Vipps.Login.Episerver`
- `Install-Package Vipps.Login.Episerver.Commerce`

### Get API keys for Vipps Log In API

Activate and set up Vipps Login: https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api-faq.md#how-can-i-activate-and-set-up-vipps-login

Configure a redirect URI to your site(s): `https://{your-site}/vipps-login` (fill in the correct url there, it can be localhost as well)

Add the ClientId and the ClientSecret to the AppSettings, as such:

```
<add key="VippsLogin:ClientId" value="..." />
<add key="VippsLogin:ClientSecret" value="..." />
<add key="VippsLogin:Authority" value="https://apitest.vipps.no/access-management-1.0/access" />
```
For production use
```
<add key="VippsLogin:Authority" value="https://api.vipps.no/access-management-1.0/access" />
```
See https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api.md#base-urls

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

#### 3. Configure Vipps OIDC during app Startup

Here you can find the default configuration needed to support Vipps OIDC. Some tips:

1. Be sure to configure only the scopes you actually need.
2. If authentication fails, we suggest redirecting to the normal login page and show an informational message.
3. Determine what you which information from Vipps you want to sync. By default we will update the customer contact and the customer addresses upon login.

```csharp
public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        // Enable the application to use a cookie to store information for the signed in user
        app.UseCookieAuthentication(new CookieAuthenticationOptions
        {
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            LoginPath = new PathString("/util/login.aspx")
        });

        // Vipps OIDC configuration starts here
        // This should match CookieAuthentication AuthenticationType above ^
        app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ApplicationCookie);
        app.UseOpenIdConnectAuthentication(new VippsOpenIdConnectAuthenticationOptions(
            VippsLoginConfig.ClientId,
            VippsLoginConfig.ClientSecret,
            VippsLoginConfig.Authority
            )
        {
            // 1. Here you pass in the scopes you need
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
            // DefaultSecurityTokenValidated - Find matching CustomerContact

            Notifications = new VippsEpiNotifications
            {
                AuthenticationFailed = context =>
                {
                    _logger.Error("Vipps.Login failed", context.Exception);

                    var message = "Something went wrong. Please contact customer support.";
                    switch (context.Exception)
                    {
                        case VippsLoginDuplicateAccountException _:
                            message = "Multiple accounts found matching this Vipps user info. Please log in and link your Vipps account through the profile page.";
                            break;
                        case VippsLoginSanityCheckException _:
                            message = "Existing account found but did not pass Vipps sanity check. Please log in and link your Vipps account through the profile page.";
                            break;
                    }

                    // 2. Redirect to login page and display message
                    context.HandleResponse();
                    context.Response.Redirect($"/user?error={message}");
                    return (Task)Task.FromResult<int>(0);
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

            // 3. Make sure to call SyncInfo. You can use the VippsSyncOptions to determine what to sync (contact/address info)
            ServiceLocator.Current.GetInstance<IVippsLoginCommerceService>()
                .SyncInfo(
                    ctx.Authentication.User.Identity,
                    CustomerContext.Current.CurrentContact,
                    new VippsSyncOptions());

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

        // Required for AntiForgery to work
        // Otherwise it'll throw an exception about missing claims
        AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
    }
}
```

When the user goes to `https://{your-site}/vipps-login`, the Vipps middleware will be triggered and it will redirect the user to the Vipps log in environment. You will have to configure this redirect URL in Vipps, as described here: https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api-faq.md#how-can-i-activate-and-set-up-vipps-login

You can add a ReturnUrl to redirect the user once they are logged in, for example `https://{your-site}/vipps-login?ReturnUrl=/vipps-landing`.

Vipps is using the OpenIdConnect Authorization Code Grant flow, this means the user is redirected back to your environment with a Authorization token. The middleware will validate the token and exchange it for an `id_token` and an `access_token`. A `ClaimsIdentity` will be created which will contain the information of the scopes that you configured (email, name, addresses etc).

### Accessing Vipps user data

The Vipps UserInfo can be accessed by calling `IVippsLoginService.GetVippsUserInfo(IIdentity identity)`, this will give you the user info that was retrieved when the user logged in (cached).

### Syncing Vipps user data

You may want to store the Vipps data in your database, for example on the Episerver CustomerContact. First, make sure you can access the data you're looking for by configuring the correct scope in your Startup class, for example for Vipps addresses add the `VippsScopes.Address` scope. Once the user has logged in, their `ClaimsIdentity` will contain all the Vipps data you have requested through the scopes. To retrieve these addresses you can use the same `IVippsLoginService.GetVippsUserInfo(IIdentity identity)` to retrieve their UserInfo; including the addresses. If you're using Episerver Commerce, install `Vipps.Login.Episerver.Commerce` and take a look at the Epi page controller example below:

```csharp
public class VippsPageController : PageController<VippsPage>
{
    private readonly IVippsLoginService _vippsLoginService;
    private readonly CustomerContext _customerContext;
    public VippsPageController(IVippsLoginService vippsLoginService, CustomerContext customerContext)
    {
        _vippsLoginService = vippsLoginService;
        _customerContext = customerContext;
    }

    public ActionResult Index(VippsPage currentPage)
    {
        SyncPersonalInfo(User.Identity, _customerContext.CurrentContact);
        return View();
    }

    private void SyncPersonalInfo(IIdentity identity, CustomerContact currentContact)
    {
        if (identity == null)
            throw new ArgumentNullException(nameof(identity));
        if (currentContact == null)
            throw new ArgumentNullException(nameof(currentContact));

        // Retrieve Vipps user info
        var vippsUserInfo = _vippsLoginService.GetVippsUserInfo(identity);
        if (vippsUserInfo == null)
        {
            return;
        }

        // Maps PII fields onto customer contact:
        // Vipps subject guid, email, firstname, lastname, fullname, birthdate
        currentContact.MapVippsUserInfo(vippsUserInfo);

        // Sync addresses
        foreach (var vippsAddress in vippsUserInfo.Addresses)
        {
            // Vipps addresses don't have an ID
            // They can be identifier by Vipps address type
            var address =
                currentContact.ContactAddresses.FindVippsAddress(vippsAddress.AddressType);
            var isNewAddress = address == null;
            if (isNewAddress)
            {
                address = CustomerAddress.CreateInstance();
                address.AddressType = CustomerAddressTypeEnum.Shipping;
            }

            // Maps fields onto customer address:
            // Vipps address type, street, city, postalcode, countrycode
            address.MapVippsAddress(vippsAddress);

            if (isNewAddress)
            {
                currentContact.AddContactAddress(address);
            }
            else
            {
                currentContact.UpdateContactAddress(address);
            }
        }

        currentContact.SaveChanges();
    }
}

```

How you store the data is up to you, of course you don't have to store it, you can just extract it from the users' identity as long as they log in using Vipps.

## More info

- https://github.com/vippsas/vipps-login-api
- https://github.com/vippsas/vipps-developers
- https://openid.net/specs/openid-connect-core-1_0.html#CodeFlowAuth
- https://world.episerver.com/documentation/developer-guides/commerce/security/support-for-openid-connect-in-episerver-commerce/

## Package maintainer

https://github.com/brianweet

## Changelog

[Changelog](CHANGELOG.md)
