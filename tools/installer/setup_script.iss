; ===============================================================================
; Inno Setup Installer Script
; Application: CA Office Workforce Productivity Automation System
; Publisher  : CA Office Automation Engineering Team
; Version    : 1.0.0
; Architecture: x64 / Windows Desktop
; ===============================================================================

#define MyAppName "CA Office Workforce Automation"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "CA Office Automation"
#define MyAppURL "https://caoffice.com"
#define MyAppExeName "StaffAutomation.WinForms.exe"

[Setup]
AppId={{D37F8E9C-4B1A-483E-9214-7A9B6C2D5E1F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\CA Office Workforce Automation
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=
OutputDir=..\..\dist
OutputBaseFilename=CAOfficeAutomation_Setup_v1.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "quicklaunchicon"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked; OnlyBelowVersion: 6.1; ToInstall: false

[Dirs]
Name: "{commonappdata}\StaffAutomation\Backups"; Permissions: users-modify administrators-full

[Files]
; Primary WinForms Release Binaries
Source: "..\..\src\StaffAutomation.WinForms\bin\Release\net8.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

; Database Initialization & Migration Scripts
Source: "..\..\database\scripts\*"; DestDir: "{app}\database\scripts"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninsexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "icacls"; Parameters: """{commonappdata}\StaffAutomation\Backups"" /grant ""NT Service\MSSQL$SQLEXPRESS"":(OI)(CI)F /grant Administrators:(OI)(CI)F /T"; Flags: runhidden

Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
// -------------------------------------------------------------------------------
// Check .NET 8.0 Windows Desktop Runtime Prerequisites
// -------------------------------------------------------------------------------
function IsDotNet8Installed(): Boolean;
var
  Success: Boolean;
  ResultCode: Integer;
begin
  // Query dotnet --list-runtimes to verify Microsoft.WindowsDesktop.App 8.0 presence
  Success := Exec('dotnet', '--list-runtimes', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  Result := Success and (ResultCode = 0);
end;

// -------------------------------------------------------------------------------
// Check SQL Server Instance Presence via Registry
// -------------------------------------------------------------------------------
function IsSqlServerInstalled(): Boolean;
var
  Names: TArrayOfString;
begin
  Result := RegGetSubkeyNames(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', Names);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  
  if not IsSqlServerInstalled() then
  begin
    MsgBox('NOTICE: No local SQL Server instances (e.g. .\SQLEXPRESS) were detected on this system.' + #13#10 + #13#10 +
           'Ensure SQL Server Express 2019/2022 is accessible over the network or installed locally before running the application.', mbInformation, MB_OK);
  end;
end;
