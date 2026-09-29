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

[Dirs]
Name: "{commonappdata}\SHIFTWorkforce"; Permissions: users-modify
; SECURITY TRADE-OFF: Giving 'users' modify permissions on this folder allows normal non-admin 
; Windows users to update dbconnection.json. This is required for the application to function 
; seamlessly across standard workgroup accounts, but it means any local user can theoretically 
; alter the connection pointer.

[Files]
Source: "..\src\StaffAutomation.WinForms\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,*.vb,*.sln,*.vbproj,dbconnection.json,appsettings.example.json,*.tmp,*Tests.dll"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "SQLEXPR_x64_ENU.exe"; DestDir: "{tmp}"; Flags: ignoreversion deleteafterinstall

[Icons]
Name: "{autoprograms}\SHIFT Workforce"; Filename: "{app}\StaffAutomation.WinForms.exe"
Name: "{autodesktop}\SHIFT Workforce"; Filename: "{app}\StaffAutomation.WinForms.exe"; Tasks: desktopicon

[Run]
; SQL Server Offline Installer
Filename: "{tmp}\SQLEXPR_x64_ENU.exe"; Parameters: "/Q /ACTION=Install /INSTANCENAME=SQLEXPRESS /FEATURES=SQLENGINE /SECURITYMODE=SQL /SAPWD=""{code:GetSAPassword}"" /SQLSVCACCOUNT=""NT AUTHORITY\Network Service"" /SQLSYSADMINACCOUNTS=""BUILTIN\ADMINISTRATORS"" /TCPENABLED=1 /NPENABLED=0 /UpdateEnabled=0 /IACCEPTSQLSERVERLICENSETERMS"; StatusMsg: "Installing SQL Server Express (This may take several minutes)..."; Check: NeedsSqlInstall; Flags: waituntilterminated

; Firewall rule
Filename: "netsh"; Parameters: "advfirewall firewall add rule name=""SHIFT SQL Server"" dir=in action=allow protocol=TCP localport=1433"; StatusMsg: "Configuring Firewall..."; Check: IsServerInstall; Flags: runhidden

; Restart SQL Service to apply TCP/IP port changes
Filename: "net"; Parameters: "stop MSSQL$SQLEXPRESS"; StatusMsg: "Stopping SQL Server..."; Check: IsServerInstall; Flags: runhidden waituntilterminated
Filename: "net"; Parameters: "start MSSQL$SQLEXPRESS"; StatusMsg: "Starting SQL Server..."; Check: IsServerInstall; Flags: runhidden waituntilterminated

; Create SQL Login shift_app using .NET SqlClient in PowerShell via Windows Auth
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{tmp}\setup_db.ps1"""; BeforeInstall: CreateSqlScript; StatusMsg: "Configuring Database Login..."; Check: IsServerInstall; Flags: runhidden waituntilterminated

Filename: "{app}\StaffAutomation.WinForms.exe"; Description: "{cm:LaunchProgram,SHIFT Workforce}"; Flags: nowait postinstall skipifsilent

[Registry]
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQLServer\SuperSocketNetLib\Tcp\IPAll"; ValueType: string; ValueName: "TcpDynamicPorts"; ValueData: ""; Check: IsServerInstall
Root: HKLM; Subkey: "SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQLServer\SuperSocketNetLib\Tcp\IPAll"; ValueType: string; ValueName: "TcpPort"; ValueData: "1433"; Check: IsServerInstall

[Code]
var
  InstallModePage: TInputOptionWizardPage;
  ClientInfoPage: TInputQueryWizardPage;
  GlobalSAPassword: String;
  GlobalAppPassword: String;

function GenerateSecurePassword(): String;
var
  PSCmd, TempFile, PasswordStr: String;
  ResultCode: Integer;
begin
  TempFile := ExpandConstant('{tmp}\rndpwd.txt');
  PSCmd := '-NoProfile -Command "$chars = ''ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#%^*-_=+''.ToCharArray(); $bytes = New-Object byte[] 24; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes); $pwd = -join ($bytes | ForEach-Object { $chars[$_ % $chars.Length] }); Set-Content -Path ''' + TempFile + ''' -Value $pwd -NoNewline"';
  
  Exec('powershell.exe', PSCmd, '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  
  if (LoadStringFromFile(TempFile, PasswordStr) = False) or (Length(PasswordStr) < 24) then
  begin
    DeleteFile(TempFile);
    MsgBox('Critical Error: Failed to generate secure password. Installation must abort.', mbError, MB_OK);
    RaiseException('Failed to generate secure password.');
  end;
  
  DeleteFile(TempFile);
  Result := PasswordStr;
end;

function GetSAPassword(Param: String): String;
begin
  if GlobalSAPassword = '' then
  begin
    GlobalSAPassword := GenerateSecurePassword();
  end;
  Result := GlobalSAPassword;
end;

function GetAppPassword(): String;
begin
  if GlobalAppPassword = '' then
  begin
    GlobalAppPassword := GenerateSecurePassword();
  end;
  Result := GlobalAppPassword;
end;

function IsServerInstall(): Boolean;
begin
  Result := True;
  if InstallModePage <> nil then
    Result := (InstallModePage.SelectedValueIndex = 0);
end;

function IsClientInstall(): Boolean;
begin
  Result := False;
  if InstallModePage <> nil then
    Result := (InstallModePage.SelectedValueIndex = 1);
end;

function IsSqlExpressInstalled(): Boolean;
var
  SqlVersion: String;
begin
  Result := RegQueryStringValue(HKLM, 'SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL', 'SQLEXPRESS', SqlVersion);
end;

function NeedsSqlInstall(): Boolean;
begin
  Result := IsServerInstall() and not IsSqlExpressInstalled();
end;

procedure InitializeWizard;
begin
  InstallModePage := CreateInputOptionPage(wpSelectDir,
    'Installation Mode', 'Select whether this is the Server or a Client PC',
    'Is this the main Server PC (where the shared database will live), or a Client PC that connects to an existing server?',
    True, False);
  InstallModePage.Add('Server (host the database here)');
  InstallModePage.Add('Client (connect to an existing server)');
  InstallModePage.Values[0] := True;

  ClientInfoPage := CreateInputQueryPage(InstallModePage.ID,
    'Server Connection Details', 'Enter the Server details to connect',
    'Please enter the Server PC''s name or IP address, and the database password.');
  ClientInfoPage.Add('Server PC Name or IP:', False);
  ClientInfoPage.Add('Database Password:', True);
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (PageID = ClientInfoPage.ID) and IsServerInstall() then
    Result := True;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  
  if (CurPageID = ClientInfoPage.ID) and IsClientInstall() then
  begin
    if (Trim(ClientInfoPage.Values[0]) = '') or (Trim(ClientInfoPage.Values[1]) = '') then
    begin
      MsgBox('Please enter both the Server Name/IP and the Database Password.', mbError, MB_OK);
      Result := False;
    end
    else if Pos('"', ClientInfoPage.Values[1]) > 0 then
    begin
      MsgBox('Database Password cannot contain double quotes (").', mbError, MB_OK);
      Result := False;
    end;
  end;
end;

procedure CreateSqlScript();
var
  Lines: TArrayOfString;
begin
  SetArrayLength(Lines, 10);
  Lines[0] := '$conn = New-Object System.Data.SqlClient.SqlConnection("Server=localhost\SQLEXPRESS;Database=master;Integrated Security=True;TrustServerCertificate=True")';
  Lines[1] := '$conn.Open()';
  Lines[2] := '$cmd = $conn.CreateCommand()';
  Lines[3] := '$cmd.CommandText = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = ''StaffAutomationDb'') CREATE DATABASE StaffAutomationDb;"';
  Lines[4] := '$cmd.ExecuteNonQuery() | Out-Null';
  Lines[5] := '$cmd.CommandText = "IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = ''shift_app'') CREATE LOGIN shift_app WITH PASSWORD = ''' + GetAppPassword() + ''';"';
  Lines[6] := '$cmd.ExecuteNonQuery() | Out-Null';
  Lines[7] := '$cmd.CommandText = "USE StaffAutomationDb; IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = ''shift_app'') CREATE USER shift_app FOR LOGIN shift_app; ALTER ROLE db_owner ADD MEMBER shift_app;"';
  Lines[8] := '$cmd.ExecuteNonQuery() | Out-Null';
  Lines[9] := '$conn.Close()';
  SaveStringsToFile(ExpandConstant('{tmp}\setup_db.ps1'), Lines, False);
end;

procedure CreateDbConnectionJson();
var
  JsonContent: String;
  ServerStr, PwdStr, EncryptedPwdStr, PwdFile: String;
  ResultCode: Integer;
begin
  if IsServerInstall() then
  begin
    ServerStr := 'localhost,1433';
    PwdStr := GetAppPassword();
  end
  else
  begin
    ServerStr := ClientInfoPage.Values[0] + ',1433';
    PwdStr := ClientInfoPage.Values[1];
  end;

  StringChangeEx(PwdStr, '''', '''''', True);

  PwdFile := ExpandConstant('{tmp}\pwd.txt');

  Exec('powershell.exe', 
       '-NoProfile -Command "Add-Type -AssemblyName System.Security; $b = [System.Text.Encoding]::UTF8.GetBytes(''' + PwdStr + '''); $e = [System.Security.Cryptography.ProtectedData]::Protect($b, $null, ''LocalMachine''); [Convert]::ToBase64String($e) | Out-File -FilePath ''' + PwdFile + ''' -Encoding ascii -NoNewline"',
       '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
       
  if (LoadStringFromFile(PwdFile, EncryptedPwdStr) = False) or (Trim(EncryptedPwdStr) = '') then
  begin
    DeleteFile(PwdFile);
    MsgBox('Critical Error: Failed to secure database password. Installation must abort.', mbError, MB_OK);
    RaiseException('Failed to secure database password.');
  end;
  
  DeleteFile(PwdFile);

  JsonContent := '{' + #13#10 +
                 '  "Server": "' + ServerStr + '",' + #13#10 +
                 '  "Instance": "",' + #13#10 +
                 '  "Port": 0,' + #13#10 +
                 '  "Database": "StaffAutomationDb",' + #13#10 +
                 '  "UseIntegratedSecurity": false,' + #13#10 +
                 '  "UserId": "shift_app",' + #13#10 +
                 '  "EncryptedPassword": "' + EncryptedPwdStr + '",' + #13#10 +
                 '  "ConnectTimeout": 15,' + #13#10 +
                 '  "TrustServerCertificate": true,' + #13#10 +
                 '  "Encrypt": false' + #13#10 +
                 '}';
                 
  ForceDirectories(ExpandConstant('{commonappdata}\SHIFTWorkforce'));
  SaveStringToFile(ExpandConstant('{commonappdata}\SHIFTWorkforce\dbconnection.json'), JsonContent, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    CreateDbConnectionJson();
    
    if IsServerInstall() then
    begin
      MsgBox('Server setup complete. Note these details — you will need them when installing SHIFT Workforce on other PCs in this office:' + #13#10 + #13#10 +
             'Server Name/IP: ' + GetComputerNameString() + #13#10 +
             'Database Password: ' + GetAppPassword(), mbInformation, MB_OK);
    end;
  end;
end;

function IsDotNet8DesktopInstalled(): Boolean;
var
  FindRec: TFindRec;
  InstallPath: String;
begin
  Result := False;
  if not RegQueryStringValue(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64', 'InstallLocation', InstallPath) then
  begin
    InstallPath := ExpandConstant('{pf64}\dotnet');
  end;
  if Copy(InstallPath, Length(InstallPath), 1) = '\' then
    InstallPath := Copy(InstallPath, 1, Length(InstallPath) - 1);
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
  if not IsDotNet8DesktopInstalled() then
  begin
    MsgBox('Microsoft .NET 8 Desktop Runtime (x64) is required to run this application. Please install it and run the setup again.', mbError, MB_OK);
    Result := False;
  end;
end;
