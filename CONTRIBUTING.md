# Contributing to Kite Glance

Thanks for considering a contribution. This is a small, opinionated
desktop widget, so the bar for changes is "does this make the product
feel more like a first-party app" — not "does this add a feature."

## Before you start

For anything beyond a small fix, **open an issue first** describing what
you want to change and why. This saves everyone time if the direction
doesn't fit the project.

## Development setup

**Prerequisites**

- Windows 11 (22H2+ recommended — see the [Acrylic material](#windows-version)
  note below)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3](https://www.python.org/downloads/) — required to run `scripts/preflight.py` before opening a PR
- A Kite Connect app — see [README § Configuration](README.md#configuration)

```powershell
git clone https://github.com/<your-username>/kite-glance.git
cd kite-glance

# Optional: set your Kite Connect key/secret as environment variables.
# (You can also just enter them in the app's Settings dialog on first run.)
# The app reads real env vars, NOT a .env file -- see README for a snippet
# that loads .env into your shell if you prefer keeping them in a file.
$env:KITE_API_KEY = "your_key"
$env:KITE_API_SECRET = "your_secret"

cd src/KiteGlance
dotnet restore
dotnet run
```

### Windows version

The acrylic backdrop requires Windows 11 22H2+
(`DwmSetWindowAttribute` / `DWMSBT_TRANSIENTWINDOW`). On older builds the
app falls back to a solid dark background automatically — this is
intentional, not a bug, so don't "fix" the fallback path away.

## Code style

- **C# files must be pure ASCII.** No em dashes, no smart quotes, no
  currency symbols as literal characters — use escapes (`\u20B9` for ₹,
  `\u2212` for a true minus sign). This project has been bitten more than
  once by Windows PowerShell mangling UTF-8 on save; keeping source ASCII
  makes that class of bug impossible.
- **XAML resource keys**: run `scripts/preflight.py` (see below) before
  opening a PR. `dotnet build` cannot catch a missing `StaticResource` —
  that only fails at runtime, on first paint, with a stack trace that
  doesn't point at your diff.
- Match the existing two-space-outside/four-space-inside XAML indentation
  and the comment style (a short "why", not a restatement of the code).
- `UseWindowsForms=true` (needed for the tray icon) puts `System.Drawing`
  in scope everywhere, which shadows `Brush`, `Color`, `Point`, `Font`,
  `KeyEventArgs`, and others. Any new file touching WPF visuals needs
  explicit `using X = System.Windows.Media.X;` aliases — see the top of
  `MainWindow.xaml.cs` for the pattern.

## Before opening a PR

Run the pre-flight check. It catches the failure classes that `dotnet
build` cannot, because XAML resource resolution happens at runtime, not
compile time:

```powershell
python scripts/preflight.py
```

It checks:

- Every `.xaml` file parses as valid XML
- Every `StaticResource` / `FindResource` reference resolves to a real key
- Every `Click=` / event handler in XAML exists in the code-behind
- All `.cs` files are pure ASCII
- Obvious `System.Drawing` / `System.Windows` type collisions

Then run the unit tests, and confirm it builds and runs:

```powershell
dotnet test tests/KiteGlance.Tests

cd src/KiteGlance
dotnet build -c Debug
dotnet run -c Debug
```

If you touch anything in `PnlMath.cs` — the P&L arithmetic — add or update a
test in `tests/KiteGlance.Tests/PnlMathTests.cs`. That file exists because
this exact logic shipped three separate bugs; the tests are what keep them
from coming back. The test project is plain `net8.0` and runs anywhere.

> **On `.env`:** the app reads real process environment variables and does
> not parse `.env` at runtime. `dotnet run` executes from `src/KiteGlance/`,
> so a `.env` at the repo root is not picked up automatically — either enter
> credentials in the Settings dialog, or load `.env` into your shell before
> running (see [README § Environment Variables](README.md#environment-variables)).

## Design principles this project holds to

If your change conflicts with one of these, it'll likely get pushback in
review — not because the idea is bad, but because it's probably a
different project:

- **Two motion laws, deliberately different.** Layout springs (a
  `SpringEase` damped-harmonic-oscillator, not a bezier) — overshoot
  reads as weight. Numbers ease, never overshoot — a rupee value should
  never render, then correct itself.
- **No invented facts.** If Kite hasn't priced a holding yet, say so —
  don't treat `last_price: 0` as "worth nothing."
- **A chart's color must agree with the number next to it.** The
  Invested→Current delta bar is colored by *its own* movement, never by
  an unrelated hero figure.
- **Native material over faked glass.** DWM acrylic via
  `DwmSetWindowAttribute`, not `AllowsTransparency=true` + a hand-painted
  gradient (which disables DWM and blurs text).
- **State persists.** Position, tab, expanded/collapsed, pin mode. A
  widget that forgets where you put it is just a small window.

## Reporting bugs

Open an issue with:

- Windows version (`winver`) and CPU architecture (ARM64 / x64)
- What you expected vs. what happened
- The exact error text, if any (check `%APPDATA%\KiteGlance` for logs
  won't help yet — there's no file logging; the Debug console output is
  what there is)

## License

By contributing, you agree your contributions are licensed under this
project's [MIT License](LICENSE).
