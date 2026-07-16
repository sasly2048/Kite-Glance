; ══════════════════════════════════════════════════════════════════════
;  Kite Glance - Inno Setup script
;
;  Produces a proper KiteGlance-Setup.exe: the double-click, click-next,
;  shows-up-in-Add/Remove-Programs kind of installer.
;
;  Build:
;    1. Install Inno Setup:  winget install JRSoftware.InnoSetup
;    2. Build the app:       .\build.ps1
;    3. Compile:             iscc setup.iss
;
;  Output: installer\KiteGlance-Setup.exe
; ══════════════════════════════════════════════════════════════════════

#define AppName        "Kite Glance"
#define AppVersion     "1.0.0"
#define AppPublisher   "KiteGlance"
#define AppExe         "KiteGlance.exe"

[Setup]
AppId={{7C4E1A93-2F8B-4D6E-9A15-3E0B7C9D4F21}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
VersionInfoVersion={#AppVersion}

; Per-user install: no admin rights, no UAC prompt. Same choice VS Code
; and Slack make. The app writes only to its own folder and HKCU.
PrivilegesRequired=lowest
DefaultDirName={localappdata}\Programs\KiteGlance
DisableDirPage=yes
DisableProgramGroupPage=yes

DefaultGroupName={#AppName}
UninstallDisplayName={#AppName}
UninstallDisplayIcon={app}\{#AppExe}

OutputDir=..\installer
OutputBaseFilename=KiteGlance-Setup
SetupIconFile=..\src\KiteGlance\app.ico

Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern

; Snapdragon runs the ARM64 binary natively; on Intel this line is the only
; thing to change (arm64 -> x64), plus the matching build.
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; \
    Description: "Create a &desktop shortcut"; \
    GroupDescription: "Shortcuts:"

Name: "startupicon"; \
    Description: "&Start Kite Glance when Windows starts"; \
    GroupDescription: "Startup:"

[Files]
Source: "..\src\KiteGlance\dist\{#AppExe}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExe}"; Tasks: desktopicon

[Registry]
; Start with Windows, only if the user asked for it.
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; \
    ValueType: string; ValueName: "KiteGlance"; \
    ValueData: """{app}\{#AppExe}"""; \
    Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#AppExe}"; \
    Description: "Launch {#AppName}"; \
    Flags: nowait postinstall skipifsilent

[UninstallDelete]
; The exe unpacks itself into a temp dir on each run (single-file publish).
Type: filesandordirs; Name: "{app}"

[Code]
// Kill a running instance before installing over it, otherwise the copy
// fails on a file lock and the user gets a cryptic error.
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  Exec('taskkill.exe', '/F /IM {#AppExe}', '', SW_HIDE,
       ewWaitUntilTerminated, ResultCode);
  Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
    Exec('taskkill.exe', '/F /IM {#AppExe}', '', SW_HIDE,
         ewWaitUntilTerminated, ResultCode);
end;
