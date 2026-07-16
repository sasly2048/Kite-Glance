# Kite Glance

A native Windows desktop widget that shows your Zerodha portfolio at a glance — live P&L, per-holding breakdown, and day change — glued to your desktop the way a widget should be.

Built with WPF on .NET 8, self-contained (no runtime to install), dependency-light, and designed for ARM64 (Snapdragon X Elite) with an x64 build alongside it.

[![Build](https://github.com/sasly2048/kite-glance/actions/workflows/build.yml/badge.svg)](https://github.com/sasly2048/kite-glance/actions/workflows/build.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-informational.svg)](LICENSE)

> **Not affiliated with Zerodha.** This is an independent, open-source client for the public Kite Connect API. See the [Disclaimer](#disclaimer).

---

## Overview

Kite Glance sits on your desktop and keeps a quiet, always-current view of your holdings. It reads your portfolio through the official Kite Connect API, encrypts your credentials locally with Windows DPAPI, and talks to nothing else — there is no backend, no telemetry, and no third-party server in the loop.

It was built to feel like a first-party part of Windows rather than a browser tab: real DWM material, spring-based motion, a rendered backdrop, and state that persists across restarts.

## Key Features

- **Live portfolio P&L** — overall and per-holding, split into Stocks and Funds tabs.
- **Accurate mutual-fund NAVs** — Kite's holdings endpoint returns a stale settlement NAV for funds; Kite Glance overrides it with the official live NAV from [AMFI](https://www.amfiindia.com), so the numbers match what Coin shows.
- **Pin to desktop** — reparents into the wallpaper layer, so the widget sits *under* your apps, appears on every virtual desktop, and survives Alt+Tab, Win+D, and trackpad gestures. "Always on top" and "Float freely" are also available.
- **Native material** — DWM acrylic corners, dark frame, and shadow via `DwmSetWindowAttribute`, plus a procedurally-rendered mesh-gradient backdrop.
- **Considered motion** — a spring easing system for layout, a separate quartic ease for numbers (money never overshoots), a skeleton loading state, and a "live" indicator that only pulses while the market is open.
- **Honest about staleness** — if a sync fails or live NAVs are unavailable, the widget says so rather than showing numbers that quietly disagree.
- **Secure by construction** — credentials encrypted at rest with Windows DPAPI (per-user scope, app-specific entropy); OAuth captured on a loopback socket with no admin rights.
- **Keyboard-friendly** — expand/collapse, refresh, and tab-switch all have shortcuts; focus rings appear for keyboard users.
- **Persistent** — remembers position, expanded/collapsed state, active tab, and pin mode.

## Technology Stack

| Layer | Choice |
|---|---|
| UI framework | WPF (`net8.0-windows`) |
| Language | C# 12 |
| Rendering / material | DWM interop (`DwmSetWindowAttribute`), procedural PNG backdrop |
| Tray + desktop glue | Win32 / WinForms `NotifyIcon`, `SetParent` into WorkerW |
| Credential storage | Windows DPAPI (`System.Security.Cryptography.ProtectedData`) |
| Market data | Kite Connect v3 REST API, AMFI NAVAll.txt |
| Auth | Kite Connect OAuth via loopback `TcpListener` |
| Packaging | `dotnet publish` single-file self-contained; Inno Setup installer |
| CI | GitHub Actions (ARM64 + x64 matrix) |

There are **no external UI or HTTP libraries** — only the .NET base class library.

## Requirements

- **Windows 11** (22H2 or newer recommended — see below)
- **ARM64 or x64** CPU
- A **Zerodha account** with **Kite Connect API access** ([developers.kite.trade](https://developers.kite.trade))

The acrylic backdrop uses Windows 11 22H2+ APIs. On older builds the widget falls back to a solid dark surface automatically — this is intentional, not a bug.

## Installation

### Option A — download a pre-built release

1. Go to the [Releases](https://github.com/sasly2048/kite-glance/releases) page.
2. Download the `KiteGlance.exe` for your architecture (`win-arm64` or `win-x64`).
3. Run it. On first launch it will ask for your Kite Connect API credentials (see [Configuration](#configuration)).

To install it properly (Start Menu shortcut, autostart, Add/Remove Programs entry), run the installer script from a checkout, or use the Inno Setup `Setup.exe` if one is attached to the release.

### Option B — build from source

See [Building from Source](#building-from-source).

## Building from Source

**Prerequisites:** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

```powershell
git clone https://github.com/sasly2048/kite-glance.git
cd kite-glance

# Run and iterate
cd src/KiteGlance
dotnet run
```

To produce a distributable single-file executable:

```powershell
# From the repo root
.\scripts\build.ps1              # ARM64 by default
.\scripts\build.ps1 -Arch x64    # or x64
```

Output lands in `src/KiteGlance/dist/KiteGlance.exe` — one self-contained file, no runtime required on the target machine.

To install it locally (per-user, with Start Menu + Desktop shortcuts and autostart):

```powershell
.\scripts\install.ps1
.\scripts\install.ps1 -Uninstall   # to remove
```

Before opening a pull request, run the pre-flight validator — it catches XAML/resource errors that `dotnet build` cannot (see [CONTRIBUTING](CONTRIBUTING.md)):

```powershell
python scripts/preflight.py
```

## Configuration

You need a Kite Connect app to get an API key and secret:

1. Go to [developers.kite.trade](https://developers.kite.trade) and create an app.
2. Set the app's **Redirect URL** to **exactly**:
   ```
   http://127.0.0.1:5173/callback
   ```
3. Note your **API key** and **API secret**.

Provide them to Kite Glance in **either** of two ways:

- **In-app (simplest):** launch the widget and enter them in the Settings dialog. They are encrypted with DPAPI and stored under `%APPDATA%\KiteGlance\vault.bin`.
- **Environment variables / `.env`:** useful when running from source. See below.

Kite access tokens expire once daily (around 7:30 AM IST, per Kite's rules), so you'll sign in through your browser once each day.

### Environment Variables

| Variable | Purpose |
|---|---|
| `KITE_API_KEY` | Your Kite Connect API key |
| `KITE_API_SECRET` | Your Kite Connect API secret |
| `KITEGLANCE_DEBUG` | Set to `1` to dump raw API responses to `%APPDATA%\KiteGlance\api-dump.json` for troubleshooting. **This file contains your holdings in plaintext**; it is auto-deleted on the next normal launch. |
| `KITEGLANCE_PUBLISHER` | Optional. Publisher name shown in Add/Remove Programs when using `install.ps1`. |

If both environment variables are set, they take priority over the stored vault — handy for development. Copy [`.env.example`](.env.example) to `.env` and fill in your values:

```
KITE_API_KEY=YOUR_KITE_API_KEY
KITE_API_SECRET=YOUR_KITE_API_SECRET
```

`.env` is git-ignored and will never be committed.

## Usage

Launch the widget; it lives on your desktop and in the system tray.

- **Click the header** or press **Space / Enter** to expand the holdings list.
- **Tab** switches between Stocks and Funds.
- **R** refreshes now (throttled to once a minute).
- **Esc** collapses the list.
- **Click a holding row** to copy its ticker; hover for exact quantity and average price.
- **Right-click the tray icon** (or the widget's menu button) to switch pin modes, toggle autostart, refresh, or quit.

The widget refreshes automatically during market hours (Mon–Fri, 09:15–15:30 IST).

## Project Architecture

```
src/KiteGlance/
├── App.xaml(.cs)              Design system, palette, resources, single-instance guard
├── MainWindow.xaml(.cs)       The widget: layout, motion, state orchestration
├── SettingsWindow.xaml(.cs)   Credential entry
├── WidgetManager.cs           Tray icon, pin-mode menu, autostart
├── TrayTheme.cs               Owner-drawn dark tray menu
├── Motion/
│   ├── SpringEase.cs          Damped-harmonic-oscillator easing for layout
│   └── Numeral.cs             Unified number-tweening (quartic ease, no overshoot)
├── Interop/
│   ├── WindowMaterial.cs      DWM acrylic / corners / shadow
│   └── DesktopPin.cs          Reparent into the wallpaper layer (WorkerW)
├── Services/
│   ├── KiteService.cs         Kite Connect v3 client + portfolio assembly
│   ├── AmfiNavService.cs      Live mutual-fund NAVs from AMFI, cached daily
│   ├── CredentialVault.cs     DPAPI-encrypted credential + token storage
│   └── LoginServer.cs         Loopback OAuth redirect capture (port 5173)
├── State/WidgetState.cs       Persisted position / tab / pin mode
└── ViewModels/PortfolioViewModel.cs

scripts/
├── build.ps1                  Single-file self-contained publish
├── install.ps1                Per-user install / uninstall
├── setup.iss                  Inno Setup installer definition
└── preflight.py               Static checks CI and contributors run
```

**Data flow:** `KiteService` fetches `/portfolio/holdings` and `/mf/holdings`, overlays live NAVs from `AmfiNavService`, and produces a `PortfolioData` the window renders. P&L uses Kite's own figures where they exist and computes from NAV otherwise; a zero from the API is never treated as a real zero.

## Roadmap

- [ ] Optional intraday sparklines per holding
- [ ] Configurable refresh interval
- [ ] Multiple-account support
- [ ] Signed release binaries (code-signing certificate)
- [ ] Historical P&L / XIRR view

Suggestions and contributions are welcome — see [CONTRIBUTING](CONTRIBUTING.md).

## Security Considerations

- **Credentials never leave your machine.** API key, secret, and access token are encrypted at rest with Windows DPAPI (per-user scope + app-specific entropy) under `%APPDATA%\KiteGlance`.
- **No backend, no telemetry.** The app talks only to `api.kite.trade` and `amfiindia.com`. There is no analytics.
- **OAuth is loopback-only.** The redirect is captured by a `TcpListener` bound to `127.0.0.1:5173`; no admin rights or URL reservations are required, and the listener closes immediately after capture.
- **Nothing sensitive is committed.** No credentials appear anywhere in this repository; `.env` and `*.bin` are git-ignored.

For the full policy and how to report a vulnerability, see [SECURITY.md](SECURITY.md).

## Contributing

Pull requests are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) first — it covers the dev setup, the ASCII-only source rule, the WinForms/WPF aliasing gotcha, and the pre-flight check to run before submitting.

## License

Released under the [MIT License](LICENSE).

## Disclaimer

Kite Glance is an independent, community-built tool. It is **not affiliated with, endorsed by, or supported by Zerodha or AMFI.** "Zerodha", "Kite", and "Coin" are trademarks of their respective owners.

This software is provided "as is", without warranty of any kind. It is a read-only viewer for your own portfolio and places no trades. Market data may be delayed or inaccurate; **do not rely on it for trading decisions.** Always verify figures against the official Kite and Coin apps. You are responsible for complying with Zerodha's API terms of use.
