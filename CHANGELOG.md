<!-- START_METADATA
---
title: Login for ASP.NET and Episerver Changelog
sidebar_label: Changelog
sidebar_position: 100
description: All notable changes to the Login for ASP.NET and Episerver plugin will be documented in this file.
pagination_next: null
pagination_prev: null
---
END_METADATA -->

# Changelog

All notable changes to this project will be documented in this file.

## [0.3.0]

Remove `VippsScopes.ApiV2` (it's default now, removed from Vipps Login documentation as well https://github.com/vippsas/vipps-login-api/pull/114)

## [0.2.2]

Fix to ignore empty addresses

## [0.2.1]

Update `Microsoft.IdentityModel.Protocols.OpenIdConnect` to version 6.8.0

## [0.2.0]

Add support for Vipps Login V2. Add the scope `VippsScopes.ApiV2` to your Vipps OpenID configuration to switch to the new API.

## [0.1.0-alpha]

First release. This version passed security pentests.
