# Changelog

All notable changes to this project are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed

- The diagnostic API dump (which contains holdings in plaintext) is now
  deleted automatically on any normal launch, so it never outlives the
  debugging session that created it.
- Mutual-fund NAVs are cached to disk, so a cold start paints immediately
  from a same-day file instead of blocking on a ~3 MB download.
- The OAuth loopback server now reads until the HTTP request is complete,
  fixing a rare truncated-login failure when the browser split the request
  across TCP segments.

### Changed

- Credential vault now mixes app-specific entropy into DPAPI (defense in
  depth). Existing vaults are transparently re-entered once.
- Portfolio refreshes are serialized, so an automatic and a manual refresh
  can no longer interleave.
- The desktop-glue corner region is rebuilt at most once per render frame
  during expand/collapse, rather than on every size change.
- The widget now indicates when fund NAVs are delayed (AMFI unreachable),
  the same way it flags a stale portfolio sync.

### Fixed

- Mutual fund P&L now matches Coin exactly. Kite's /mf/holdings endpoint
  returns a stale settlement NAV (observed 1-3 percent off) and a literal
  pnl: 0 for every fund; the widget now fetches live NAVs from AMFI's
  official daily file (NAVAll.txt, keyed by ISIN, no auth) and overrides
  Kite's figure wherever a match exists, falling back to Kite's NAV when
  AMFI is unreachable. Funds Kite reports as unpriced (last_price: 0) are
  also resolved through AMFI when possible.

### Fixed

- Overall P&L now matches the Kite website exactly: totals are the sum of
  Kite's own per-holding `pnl` figures from the API, rather than a local
  `(last - avg) * qty` recomputation that drifts from Kite's average-price
  accounting.
- Day P&L falls back to `(last_price - close_price) * qty` when the API
  omits `day_change`.

### Added

- **Pin to desktop** mode (now the default): the widget is reparented into
  Explorer's wallpaper layer (WorkerW), so it sits under your apps, exists
  on every virtual desktop, and survives Alt+Tab, Win+D, and trackpad
  gestures. "Always on top" and "Float freely" remain available from the
  menu.

### Changed

- Replaced the acrylic/glass backdrop with a fully painted opaque surface:
  a diagonal graphite gradient with a subtle indigo ambient wash, a warm
  counter-wash, vignette, and dither grain. No DWM backdrop dependency,
  which is also what allows desktop-glued mode to render correctly.

## [1.0.0] - 2026-07-14

Initial public release.

### Added

- Native WPF desktop widget for viewing a Zerodha Kite Connect portfolio,
  built specifically for ARM64 (Snapdragon X Elite) with an x64 build
  target alongside it.
- Real system material: DWM acrylic backdrop, native rounded corners, and
  system shadow via `DwmSetWindowAttribute` — not a hand-painted
  transparent overlay.
- Spring-based motion system (`SpringEase`, a damped harmonic oscillator)
  for layout transitions, and a separate quartic ease-out for all numeric
  values, so money never visibly overshoots.
- Every numeral in the widget (hero P&L, invested, current, overall)
  animates under one unified system rather than only the headline figure.
- Centre-anchored delta bar showing Invested → Current, colored by its
  own movement rather than an unrelated headline figure.
- Honest handling of unpriced holdings: units Kite hasn't priced yet
  (`last_price: 0`) are held at cost instead of being counted as a 100%
  loss.
- Skeleton loading state shaped exactly like the content that's about to
  arrive, so nothing reflows on first paint.
- Breathing "live" indicator that only pulses while the market is
  actually open.
- Stale-data handling: if a refresh fails, the last-known figures stay on
  screen with an honest "stale Xm ago" label instead of going blank.
- Position, expanded/collapsed state, active tab, and pin preference all
  persist across restarts (`%APPDATA%\KiteGlance\state.json`).
- "Always on top" pinning, on by default, so the widget survives Alt+Tab
  and clicking other windows.
- Owner-drawn dark context menus for both the widget and the system tray
  — no default Windows/WinForms grey chrome.
- Credentials encrypted at rest with Windows DPAPI
  (`ProtectedData`, per-user scope); OAuth redirect captured by a
  loopback `TcpListener` on `127.0.0.1:5173`, no admin rights required.
- Single-instance guard via a named mutex.
- Keyboard support: `Esc` to collapse, `Space`/`Enter` to toggle,
  `R` to refresh, `Tab` to switch between Stocks and Funds.
- Click-to-copy on holding rows, with full precision (exact ticker,
  quantity, and average price) available via tooltip.
- Production build pipeline: `dotnet publish` single-file, self-contained,
  trimming intentionally disabled (WPF's XAML reflection breaks the
  trimmer), plus a per-user installer script and an Inno Setup script for
  a full `Setup.exe` with Add/Remove Programs registration.

### Security

- No secrets committed anywhere in source. API credentials are entered at
  runtime via the Settings dialog or read from environment variables
  (`KITE_API_KEY`, `KITE_API_SECRET`) — see `.env.example`.
