# Configure Vipps Log In for Episerver

Vipps Log In middleware uses OpenID Connect, so first we need to prepare Episerver for OpenID Connect.

Described in detail [here](https://world.episerver.com/documentation/developer-guides/CMS/security/integrate-azure-ad-using-openid-connect/).

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

Here, you can find the default configuration for the Vipps OIDC middleware. Some tips:

1. Be sure to configure only the scopes you actually need.
2. If authentication fails, we suggest redirecting to the normal login page and show an informational message.
3. Determine what you which information from Vipps you want to sync. By default, we will update the customer contact and the customer addresses upon login.

```csharp
public class Startup
{
    private static Injected<IVippsLoginCommerceService> VippsLoginCommerceService { get; set; }
    private static Injected<IVippsLoginService> VippsLoginService { get; set; }

    public void Configuration(IAppBuilder app)
    {
        // Enable the application to use a cookie to store information for the signed in user
        app.UseCookieAuthentication(new CookieAuthenticationOptions
        {
            AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
            LoginPath = new PathString("/util/login.aspx")
        });

        // This should match CookieAuthentication AuthenticationType above ^
        app.SetDefaultSignInAsAuthenticationType(DefaultAuthenticationTypes.ApplicationCookie);
        // Vipps OIDC configuration starts here
        app.UseOpenIdConnectAuthentication(new VippsOpenIdConnectAuthenticationOptions(
            VippsLoginConfig.ClientId,
            VippsLoginConfig.ClientSecret,
            VippsLoginConfig.Authority
            )
        {
            // 1. Here you pass in the scopes you need
            Scope = string.Join(" ", new []
            {
                VippsScopes.ApiV2,
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
            // SecurityTokenValidated - Used to populate ClaimsIdentity and sync to db
            //      Override this to implement your own logic for finding and creating accounts.
            //      See VippsEpiNotifications.DefaultSecurityTokenValidated for an example
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
                        case VippsLoginLinkAccountException accountException:
                            if (accountException.UserError)
                            {
                                message =
                                    "Existing account found with a connection to Vipps. Please remove the connection through the profile page.";
                            }
                            break;
                    }

                    // 2. Redirect to login or error page and display message
                    context.HandleResponse();
                    context.Response.Redirect($"/login?error={message}");
                    return Task.FromResult(0);
                }
            }
        });
        // Trigger Vipps middleware on this path to start authentication
        app.Map("/vipps-login", map => map.Run(ctx =>
        {
            // 3. Vipps log in and sync Vipps user info
            if (VippsLoginCommerceService.Service.HandleLogin(ctx, new VippsSyncOptions
            {
                SyncContactInfo = true,
                SyncAddresses = true
            })) return Task.Delay(0);

            // Link Vipps account to current logged in user account
            bool.TryParse(ctx.Request.Query.Get("LinkAccount"), out var linkAccount);
            if (linkAccount && VippsLoginCommerceService.Service.HandleLinkAccount(ctx)) return Task.Delay(0);

            // Return to this url after authenticating
            var returnUrl = ctx.Request.Query.Get("ReturnUrl");
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = "/";
            }
            VippsLoginCommerceService.Service.HandleRedirect(ctx, returnUrl);

            return Task.Delay(0);
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

When the user goes to `https://{your-site}/vipps-login`, the Vipps middleware will be triggered and it will redirect the user to the Vipps log in environment. You will have to configure this redirect URL in Vipps, as described [here](https://developer.vippsmobilepay.com/docs/APIs/login-api/vipps-login-api-faq/#how-can-i-activate-and-set-up-vipps-login).

You can add a `ReturnUrl` to redirect the user once they are logged in. For example `https://{your-site}/vipps-login?ReturnUrl=/vipps-landing`.

Vipps is using the OpenIdConnect Authorization Code Grant flow. This means the user is redirected back to your environment with an authorization token. The middleware will validate the token and exchange it for an `id_token` and an `access_token`. A `ClaimsIdentity` will be created which will contain the information of the scopes that you configured (email, name, addresses etc.).

### The log in and registration flow

The library implements the recommendations described by Vipps [here](https://developer.vippsmobilepay.com/docs/APIs/login-api/api-guide/important-information/#recommendations-on-linking-to-user-account):

> Even though the website might have separate entry points for registration of new users and login for existing users the functionality related to Vipps login should not differ between these two scenarios. If a new user ends up clicking "login" the merchant should create a new account and log the user into that. If an existing user clicks "register" the merchant should log the user into her existing account. This is because the user might not remember whether she has an account or not and the merchant can get the same information from Vipps login in both these cases.
>
> Normally we recommend the checks related to log in/registration to be like this:
>
> 1. First check if you already have the unique user identifier for Vipps (called "sub" in the response from our API) stored on one of your accounts. If you have it, this means that the user has used Vipps on your site earlier and have an explicit link to the account. In this case use the ID to log the user into her account.
> 2. If you have not already stored the ID: check if the user already have an account based on phone number and e-mail address. If this gives a match on one (and only one) account, then you can use this to log the user into that account since both phone number and e-mail address is verified in Vipps. Before completing the link it is an advantage to do a "sanity check" on the name of the Vipps user to the name in the existing account to make sure that the account is not an old account where the user has abandoned the phone number or e-mail address and this has been picked up by someone else at a later time.
> 3. If you get a match on multiple accounts you can provide information on this and offer the user the possibility to log in to her existing account (using the old login method) and then link the account to Vipps.
> 4. It is also recommended on "my page" or similar on the website to provide the option for logged-in users that has not yet linked their profile to Vipps to do so, for an easier login the next time. This just means to provide the "login with Vipps"-button and linking the ID from Vipps with this account.

### Link Vipps to an existing account

If you want to allow *logged in users* to link to Vipps to their existing non Vipps account, you can add a link the redirect them to `https://{your-site}/vipps-login?LinkAccount=true`. When they visit that link, they will be redirected to Vipps and can go through the log in process. Once they're redirected back to your site, their Vipps account will be linked to their existing account. This means that they will now be able to use Vipps to access their existing account and they can sync their data from Vipps to Episerver.

### Customized 'sanity check' during login

If the user tries to log in with Vipps and there is an existing account that matches the Vipps information (email or phone number), the library will execute a 'sanity check'. This is done to make sure that the account is not an old account where the user has abandoned the phone number or e-mail address and this has been picked up by someone else at a later time.
By default, it will compare the first name and the last name, however it is easy to change this behaviour by implementing a custom sanity check and registering it in the DI container:

```csharp
public class VippsLoginSanityCheck : IVippsLoginSanityCheck
{
    public bool IsValidContact(CustomerContact contact, VippsUserInfo userInfo)
    {
        // your logic here
    }
}
```

### Linking a Vipps account to multiple webshop accounts

It is not possible to link a Vipps account to multiple accounts on the webshop. The library will throw a `VippsLoginLinkAccountException` with the `UserError` property set to true. To recover from this, you can give the user the option to remove the link between the webshop account and the Vipps account. You can use the `IVippsLoginCommerceService.RemoveLinkToVippsAccount(CustomerContact contact)` method to remove the link to the existing account.

### Syncing Vipps user data

By default, the Vipps user info and the Vipps addresses will be synced during log in. If decide not to sync this data during log in, you might want to sync the data later on.
To do so you can call `IVippsLoginCommerceService.SyncInfo` and use the `VippsSyncOptions` parameter to configure what to sync:

```csharp
public class VippsPageController : PageController<VippsPage>
{
    private readonly IVippsLoginCommerceService _vippsLoginCommerceService;
    private readonly CustomerContext _customerContext;
    public VippsPageController(IVippsLoginCommerceService vippsLoginCommerceService, CustomerContext customerContext)
    {
        _vippsLoginCommerceService = vippsLoginCommerceService;
        _customerContext = customerContext;
    }

    public ActionResult Index(VippsPage currentPage)
    {
        // Sync user info and addresses
        _vippsLoginCommerceService.SyncInfo(
            User.Identity,
            _customerContext.CurrentContact,
            new VippsSyncOptions {
                SyncContactInfo = true, SyncAddresses = true
            }
        );

        return View();
    }
}
```