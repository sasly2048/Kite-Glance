#!/usr/bin/env python3
"""
Pre-flight checks for Kite Glance.

Run this before every commit that touches XAML or C#:

    python scripts/preflight.py

Why this exists: `dotnet build` passes on a XAML file with missing
StaticResource keys, because WPF resolves resource lookups at RUNTIME,
not compile time. A broken resource reference compiles clean and then
crashes on first paint with a XamlParseException. The compiler cannot
catch this class of bug -- so this script does, statically, before you
ever have to run the app to find out.
"""
import io
import os
import re
import sys
import glob
import xml.dom.minidom

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.dirname(SCRIPT_DIR)
PROJECT_DIR = os.path.join(REPO_ROOT, 'src', 'KiteGlance')


def main() -> int:
    if not os.path.isdir(PROJECT_DIR):
        print(f"error: project directory not found: {PROJECT_DIR}")
        return 1

    os.chdir(PROJECT_DIR)
    fail: list[str] = []

    xaml_files = glob.glob('*.xaml') + glob.glob('*/*.xaml')
    cs_files = glob.glob('*.cs') + glob.glob('*/*.cs')

    # 1. Every .xaml file parses as valid XML.
    for f in xaml_files:
        try:
            xml.dom.minidom.parse(f)
        except Exception as e:
            fail.append(f"XML invalid: {f}: {e}")

    # 2. XML comments are legal (no '--' inside, no trailing '-').
    for f in xaml_files:
        s = io.open(f, encoding='utf-8').read()
        for m in re.finditer(r'<!--(.*?)-->', s, re.S):
            if '--' in m.group(1) or m.group(1).endswith('-'):
                fail.append(f"Illegal XML comment in {f}")

    # 3. Every StaticResource / FindResource reference resolves to a real key.
    app_path = 'App.xaml'
    defined: set[str] = set()
    if os.path.exists(app_path):
        app = io.open(app_path, encoding='utf-8').read()
        defined = set(re.findall(r'x:Key="([\w.]+)"', app))

    for f in xaml_files:
        s = io.open(f, encoding='utf-8').read()
        local = set(re.findall(r'x:Key="([\w.]+)"', s))
        for key in sorted(set(re.findall(r'StaticResource\s+([\w.]+)', s))):
            if key not in defined and key not in local:
                fail.append(f"Missing resource '{key}' referenced in {f}")

    # Resources looked up from code-behind. Window-local resources (declared
    # inside that window's own Window.Resources, e.g. a ContextMenu) are
    # findable via FindResource but won't appear in App.xaml -- allow those
    # by name if they're defined anywhere in the project.
    all_local_keys: set[str] = set()
    for f in xaml_files:
        s = io.open(f, encoding='utf-8').read()
        all_local_keys |= set(re.findall(r'x:Key="([\w.]+)"', s))

    for f in cs_files:
        s = io.open(f, encoding='utf-8').read()
        for key in sorted(set(re.findall(r'FindResource\("([\w.]+)"\)', s))):
            if key not in defined and key not in all_local_keys:
                fail.append(f"Missing resource '{key}' referenced in {f}")

    # 4. Every Click= / MouseDown= / MouseUp= handler exists in code-behind.
    for xf in xaml_files:
        cf = xf + '.cs'
        if not os.path.exists(cf):
            continue
        xs = io.open(xf, encoding='utf-8').read()
        cs = io.open(cf, encoding='utf-8').read()
        for h in sorted(set(re.findall(r'(?:Click|MouseDown|MouseUp)="(\w+)"', xs))):
            if not re.search(r'\b(?:async\s+)?void\s+' + h + r'\s*\(', cs):
                fail.append(f"Missing handler '{h}' for {xf}")

    # 5. C# source is pure ASCII. Windows PowerShell's Get-Content/Out-File
    #    mangle UTF-8 on save; keeping source ASCII makes that bug impossible.
    for f in cs_files:
        t = io.open(f, encoding='utf-8').read()
        bad = sorted({c for c in t if ord(c) > 127})
        if bad:
            fail.append(f"Non-ASCII in {f}: {bad}")

    # 6. Likely WinForms/WPF type collisions. UseWindowsForms=true puts
    #    System.Drawing in scope everywhere via implicit usings, which
    #    shadows a dozen WPF type names.
    collide = ['Brush', 'Color', 'Point', 'Size', 'FontFamily', 'FontStyle',
               'KeyEventArgs', 'Pen', 'Rectangle', 'Font']
    for f in cs_files:
        src = io.open(f, encoding='utf-8').read()
        if 'System.Windows.Media' not in src and 'System.Drawing' not in src:
            continue
        body = re.sub(r'^\s*using .*$', '', src, flags=re.M)
        body = re.sub(r'//.*$', '', body, flags=re.M)
        aliases = set(re.findall(r'^using (\w+)\s*=', src, re.M))
        for n in collide:
            if n in aliases:
                continue
            if re.search(r'(?<![\w.])' + n + r'\s*[(\[{ ]', body):
                fail.append(f"Possible CS0104: bare '{n}' in {f} (no alias)")

    # 7. Cross-file references to MainWindow's public API actually exist.
    #    Renaming a public property (e.g. AlwaysOnTop -> Pin) compiles fine in
    #    the file you edited and breaks every OTHER file that used it.
    mw_path = 'MainWindow.xaml.cs'
    if os.path.exists(mw_path):
        mw = io.open(mw_path, encoding='utf-8').read()
        members = set(re.findall(
            r'public\s+(?:static\s+)?(?:async\s+)?[\w<>?\[\], ]+\s+(\w+)\s*[({=]', mw))
        members |= set(re.findall(r'public\s+[\w<>?]+\s+(\w+)\s*\{', mw))

        inherited = {'Dispatcher', 'Show', 'Hide', 'Activate', 'Close',
                     'WindowState', 'Closing', 'Topmost', 'Left', 'Top'}

        for f in cs_files:
            if f == mw_path:
                continue
            src = io.open(f, encoding='utf-8').read()
            for m in sorted(set(re.findall(r'_widget\.(\w+)', src))):
                if m not in members and m not in inherited:
                    fail.append(f"'{f}' calls _widget.{m}, which MainWindow does not define")

    # 8. Bare method CALLS inside MainWindow resolve to a method that exists
    #    in the file (or a known inherited/framework member). Catches calling
    #    a helper by a guessed name -- e.g. ShowToast() when the method is
    #    Flash() -- which dotnet catches but only at build time on Windows,
    #    long after this script has said everything is fine.
    if os.path.exists(mw_path):
        mw = io.open(mw_path, encoding='utf-8').read()
        defined = set(re.findall(
            r'(?:private|public|protected|internal)\s+(?:static\s+)?(?:async\s+)?'
            r'[\w<>?\[\], ]+\s+(\w+)\s*\(', mw))

        # Local functions have no access modifier: "static T Name() =>" or
        # "void Name(...)" nested in a method body.
        defined |= set(re.findall(
            r'(?:^|\s)(?:static\s+)?[\w<>?\[\]]+\s+(\w+)\s*\([^)]*\)\s*(?:=>|\{)',
            mw, re.M))

        known = {
            # inherited from Window/FrameworkElement or framework-provided
            'InitializeComponent', 'FindResource', 'TryFindResource', 'Show',
            'Hide', 'Close', 'Activate', 'DragMove', 'BeginAnimation',
            'GetTemplateChild', 'OnSourceInitialized', 'OnClosing', 'Focus',
            # BCL statics commonly called bare via using static / same class
            'Equals', 'ToString', 'GetHashCode',
        }

        # Bare calls: an identifier followed by ( at the start of an
        # expression -- not preceded by '.', 'new ', or being a definition.
        for m in sorted(set(re.findall(r'(?<![.\w])([A-Z]\w+)\s*\(', mw))):
            # Skip type names used as constructors/casts and control keywords.
            if m in defined or m in known:
                continue
            if re.search(rf'\bnew\s+{m}\s*\(', mw):
                continue
            if re.search(rf'(?:class|enum|struct)\s+{m}\b', mw):
                continue
            # Static classes and types invoked bare (e.g. Math(, Enum() don't
            # match because those are always X.Y calls; what's left that we
            # can't account for is suspicious only if it LOOKS like a local
            # helper: defined nowhere, used with lowercase-free name, and not
            # a known WPF type constructor pattern.
            if re.search(rf'\b(?:DoubleAnimation|Thickness|Duration|TimeSpan|'
                         rf'GridLength|CornerRadius|Rect|Uri|Debounce)\b', m):
                continue
            fail.append(
                f"'{mw_path}' calls {m}(...) but defines no such method "
                f"(typo or renamed helper?)")

    print()
    if fail:
        print(f"PRE-FLIGHT FAILED ({len(fail)} issue{'s' if len(fail) != 1 else ''})\n")
        for x in fail:
            print(f"  x {x}")
        print()
        return 1

    print(f"pre-flight: all checks pass "
          f"({len(xaml_files)} xaml, {len(cs_files)} cs)")
    return 0


if __name__ == '__main__':
    sys.exit(main())
