# Vipps Login

## Description

This repository contains the code to use Vipps Log In OpenIdConnect (OIDC) Authorization in your ASP.NET application. Information about the Vipps Log In API can be found here: https://github.com/vippsas/vipps-login-api

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

Here you can find the default configuration needed to support Vipps OIDC. The code in the `SecurityTokenValidated` notification will be executed once the user is logged in through Vipps and gave consent to our application. Here the identity will contain all Vipps User Information and it's up to you to decide what to do with it.
The code below will let users Authenticate themselves through Vipps:

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
            // DefaultAuthenticationFailed - Display error message on failed auth
            Notifications = new VippsOpenIdConnectAuthenticationNotifications
            {
                // This will be called after creating the identity and its roles but before syncing to the db.
                // Can be used to add/delete claims or change other properties
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
                    // identity.AddClaim(new Claim(ClaimTypes.Role, "WebEditors"));

                    // You can also create an application user here
                    // Or automatically sync all Vipps data

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

        // Required for AntiForgery to work
        // Otherwise it'll throw an exception about missing claims
        AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
    }
}
```

When the user goes to the path `https://{your-site}/vipps-login`, the Vipps middleware will be triggered and it will redirect the user to the Vipps log in environment. You will have to configure this redirect URL in Vipps, as described here: https://github.com/vippsas/vipps-login-api/blob/master/vipps-login-api-faq.md#how-can-i-activate-and-set-up-vipps-login

You can add a ReturnUrl to redirect the user once they are logged in, for example `https://{your-site}/vipps-login?ReturnUrl=/vipps-landing`.

Vipps is using the OpenIdConnect Authorization Code Grant flow, this means the user is redirected back to your environment with a Authorization token. The middleware will validate the token and exchange it for an `id_token` and an `access_token`. A `ClaimsIdentity` will be created which will contain the information of the scopes that you configured (email, name, addresses etc).

The Vipps UserInfo can be accessed by calling `IVippsLoginService.GetVippsUserInfo(ClaimsIdentity identity)`, this will give you the user info that was retrieved when the user logged in (cached).

### Syncing Vipps user data

If you want to use the Vipps data, for example the Vipps addresses, you may want to store that data in your database. First, make sure you can access the data you're looking for by configuring the correct scope in your Startup class, for example add the `VippsScopes.Address` scope. Once the user has logged in, their `ClaimsIdentity` will contain all the Vipps data you have requested through the scopes. To retrieve these addresses you can use the same `IVippsLoginService.GetVippsUserInfo(ClaimsIdentity identity)` to retrieve their UserInfo; including the addresses. If you're using Episerver commerce, install `Vipps.Login.Episerver.Commerce` and take a look at the Epi page controller example below:

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
