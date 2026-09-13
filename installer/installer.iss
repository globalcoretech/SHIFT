[Setup]
AppId={{D3F95C48-12A4-4B29-B2A8-E07C6418E9DA}
AppName=SHIFT Workforce
AppVersion=1.0.0
AppPublisher=CA Office Workforce
DefaultDirName={autopf}\CA Office Workforce\SHIFT Workforce
DisableProgramGroupPage=yes
OutputBaseFilename=SHIFTWorkforceSetup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
UninstallDisplayIcon={app}\StaffAutomation.WinForms.exe

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Package ONLY the Release publish output
; Strictly exclude source code, test assemblies, db backups, local configs, secrets, debug artifacts
Source: "..\src\StaffAutomation.WinForms\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,*.vb,*.sln,*.vbproj,dbconnection.json,appsettings.example.json,*.tmp,*Tests.dll"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\SHIFT Workforce"; Filename: "{app}\StaffAutomation.WinForms.exe"
Name: "{autodesktop}\SHIFT Workforce"; Filename: "{app}\StaffAutomation.WinForms.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\StaffAutomation.WinForms.exe"; Description: "{cm:LaunchProgram,SHIFT Workforce}"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet8DesktopInstalled(): Boolean;
var
  FindRec: TFindRec;
  InstallPath: String;
begin
  Result := False;
  
  // Determine .NET install location from registry, fallback to default Program Files path
  if not RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64', 'InstallLocation', InstallPath) then
  begin
    InstallPath := ExpandConstant('{pf64}\dotnet');
  end;
  
  // Ensure the path doesn't end with a backslash before appending
  if Copy(InstallPath, Length(InstallPath), 1) = '\' then
    InstallPath := Copy(InstallPath, 1, Length(InstallPath) - 1);
  
  // Check for the Windows Desktop App shared framework folder specifically for version 8.0.x
  if FindFirst(InstallPath + '\shared\Microsoft.WindowsDesktop.App\8.0.*', FindRec) then
  begin
    try
      repeat
        if (FindRec.Attributes and FILE_ATTRIBUTE_DIRECTORY) <> 0 then
        begin
          Result := True;
          Break;
        end;
      until not FindNext(FindRec);
    finally
      FindClose(FindRec);
    end;
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  
  // Enforce .NET 8 Desktop Runtime Prerequisite (Phase 4 requirement: detect but do not download)
  if not IsDotNet8DesktopInstalled() then
  begin
    MsgBox('Microsoft .NET 8 Desktop Runtime (x64) is required to run this application. Please install it and run the setup again.', mbError, MB_OK);
    Result := False;
  end;
end;
