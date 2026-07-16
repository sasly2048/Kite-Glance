# Security Policy

## Reporting a vulnerability

If you find a security issue in Kite Glance, please **do not open a public
issue**. Instead:

1. Go to the repository's **Security** tab on GitHub.
2. Click **Report a vulnerability** to open a private advisory.

You should get an initial response within a few days. Please include:

- A description of the issue and its potential impact
- Steps to reproduce (a minimal repro helps a lot)
- The version / commit you tested against

## Supported versions

Only the latest released version is supported with security fixes. Please
update before reporting.

## What this app does with your data

Understanding the data flow is the fastest way to reason about risk here:

- **Credentials** (Kite Connect API key and secret) are encrypted at rest
  with **Windows DPAPI**, scoped to your Windows user account
  (`ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`). They are
  stored in `%APPDATA%\KiteGlance\vault.bin` and cannot be decrypted by
  another user account on the same machine, or by copying the file to a
  different machine.
- **Access tokens** (which Kite Connect rotates daily) are stored the same
  way, in `%APPDATA%\KiteGlance\token.bin`.
- **Network access** is limited to `api.kite.trade` (portfolio data) and the
  Kite login flow in your default browser. The app makes no other outbound
  calls, and there is no analytics or telemetry.
- **The OAuth redirect** is captured by a loopback `TcpListener` bound to
  `127.0.0.1:5173`. It only accepts connections from `localhost` and shuts
  down immediately after capturing the request token.
- Nothing is ever sent to a third-party server. There is no backend for this
  project — it talks directly to Kite Connect's API from your machine.

## Reporting credential exposure

If you believe your Kite API key or access token has been exposed:

1. Regenerate your API secret at
   [developers.kite.trade](https://developers.kite.trade).
2. Revoke the affected access token from your
   [Kite account settings](https://kite.zerodha.com).
3. Clear the local vault: delete `%APPDATA%\KiteGlance\vault.bin` and
   `%APPDATA%\KiteGlance\token.bin`, then re-enter fresh credentials.

## Scope

This policy covers the Kite Glance application code in this repository. It
does not cover Zerodha's Kite Connect API or infrastructure — for issues
there, contact Zerodha directly.
