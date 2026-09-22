[Setup]
AppName=CA Office Workforce Automation
AppVersion=1.0
DefaultDirName={autopf}\CA Office Workforce Automation
DefaultGroupName=CA Office Workforce Automation
OutputBaseFilename=StaffAutomation_Setup
Compression=lzma
SolidCompression=yes
OutputDir=.\InstallerOutput

[Files]
; Ye line aapke publish kiye hue portable files ko uthayegi
Source: "src\StaffAutomation.WinForms\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Start menu aur Desktop par icon banayega
Name: "{group}\CA Office Workforce Automation"; Filename: "{app}\StaffAutomation.WinForms.exe"
Name: "{autodesktop}\CA Office Workforce Automation"; Filename: "{app}\StaffAutomation.WinForms.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"
