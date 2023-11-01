﻿using System;
using System.Security.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Vipps.Login
{
    public class VippsOpenIdConnectAuthenticationOptions : OpenIdConnectAuthenticationOptions
    {
        public VippsOpenIdConnectAuthenticationOptions(
            string clientId,
            string clientSecret,
            string authority)
        {
            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                throw new ArgumentException(
                    $"Vipps Login: API keys (clientId, clientSecret) are required. See https://developer.vippsmobilepay.com/docs/APIs/login-api/login-api-faq/#how-can-i-activate-and-set-up-vipps-login",
                    nameof(clientId));
            if (string.IsNullOrWhiteSpace(authority))
                throw new ArgumentException(
                    $"Vipps Login: Authority (base url) is required. See https://developer.vippsmobilepay.com/docs/APIs/login-api/api-guide/browser-flow-integration/",
                    nameof(authority));

            ClientId = clientId;
            ClientSecret = clientSecret;
            Authority = authority;
            AuthenticationType = VippsAuthenticationDefaults.AuthenticationType;
            AuthenticationMode = AuthenticationMode.Passive;
            ResponseType = OpenIdConnectResponseType.Code;
            ResponseMode = OpenIdConnectResponseMode.Query;
            RedeemCode = true;
            TokenValidationParameters = new TokenValidationParameters
            {
                RoleClaimType = ClaimTypes.Role
            };

            Notifications = new VippsOpenIdConnectAuthenticationNotifications();
        }
    }
}