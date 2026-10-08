; Script generated for Nexrein Print Monitor Windows Suite
#define MyAppName "Nexrein Print Monitor"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Nexrein"
#define MyAppURL "https://github.com/nexrein/printmonitor"
#define MyAppExeName "PrintMonitor.exe"
#define MyManagerExeName "PrintMonitor.Manager.exe"
#define SourcePublishDir "..\src\PrintMonitor\bin\Release\net10.0\win-x64\publish"

[Setup]
AppId={{9F7B2B16-6F80-4D2A-B981-12B64F8B388D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\PrintMonitor
DefaultGroupName=Nexrein Print Monitor
LicenseFile=LICENSE.txt
OutputDir=..\release
OutputBaseFilename=PrintMonitor-Setup
SetupIconFile=assets\app.ico
UninstallIconFile=assets\app.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyManagerExeName}
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"
Name: "autostart"; Description: "Automatically launch Nexrein Print Monitor on Windows startup"; GroupDescription: "Windows Startup:"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NexreinPrintMonitorManager"; ValueData: """{app}\{#MyManagerExeName}"""; Tasks: autostart; Flags: uninsdeletevalue

[Dirs]
Name: "{commonappdata}\PrintMonitor"; Permissions: authusers-modify
Name: "{commonappdata}\PrintMonitor\Logs"; Permissions: authusers-modify

[Files]
Source: "{#SourcePublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "assets\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\app.png"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Nexrein Print Monitor Control Panel"; Filename: "{app}\{#MyManagerExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\Nexrein Print Monitor Status (CLI)"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--status"
Name: "{group}\Nexrein Print Monitor Printers (CLI)"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--printers"
Name: "{group}\Uninstall Nexrein Print Monitor"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Nexrein Print Monitor"; Filename: "{app}\{#MyManagerExeName}"; Tasks: desktopicon; IconFilename: "{app}\app.ico"

[Run]
; 1. Register Service with sc.exe
Filename: "{sys}\sc.exe"; Parameters: "create ""PrintMonitor"" binPath= ""\""{app}\{#MyAppExeName}\"""" start= auto DisplayName= ""Nexrein Print Monitor - Print Monitoring Agent"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering Windows Service..."
; 2. Set Description
Filename: "{sys}\sc.exe"; Parameters: "description ""PrintMonitor"" ""Monitors Windows print jobs and synchronizes print activity with the Nexrein Print Monitor management system."""; Flags: runhidden waituntilterminated
; 3. Configure Recovery
Filename: "{sys}\sc.exe"; Parameters: "failure ""PrintMonitor"" reset= 86400 actions= restart/60000/restart/60000/restart/60000"; Flags: runhidden waituntilterminated
; 4. Start Service
Filename: "{sys}\sc.exe"; Parameters: "start ""PrintMonitor"""; Flags: runhidden waituntilterminated; StatusMsg: "Starting Nexrein Print Monitor Service..."
; 5. Launch Manager GUI (optional postinstall)
Filename: "{app}\{#MyManagerExeName}"; Description: "Launch Nexrein Print Monitor Control Panel"; Flags: postinstall nowait skipifsilent

[UninstallRun]
; Stop and delete service before removing files
Filename: "{sys}\sc.exe"; Parameters: "stop ""PrintMonitor"""; Flags: runhidden waituntilterminated; RunOnceId: "StopPrintMonitorService"
Filename: "{sys}\sc.exe"; Parameters: "delete ""PrintMonitor"""; Flags: runhidden waituntilterminated; RunOnceId: "DeletePrintMonitorService"

[Code]
var
  ApiPage: TInputQueryWizardPage;

procedure InitializeWizard;
begin
  ApiPage := CreateInputQueryPage(
    wpSelectDir,
    'Nexrein Print Monitor API Configuration',
    'Enter the remote Laravel management server connection details.',
    'Please enter the API base URL and your authentication key or token. These can also be configured later in C:\ProgramData\PrintMonitor\config.json.');

  ApiPage.Add('API Base URL (e.g., https://your-domain.com/api):', False);
  ApiPage.Add('API Key / Device Token (optional):', False);

  // Default values
  ApiPage.Values[0] := 'https://your-domain.com/api';
  ApiPage.Values[1] := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigPath: String;
  ConfigContent: String;
  ApiUrlVal: String;
  ApiKeyVal: String;
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Safely stop and delete any existing service before copying files to prevent file locking
    Exec(ExpandConstant('{sys}\sc.exe'), 'stop "PrintMonitor"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000);
    Exec(ExpandConstant('{sys}\sc.exe'), 'delete "PrintMonitor"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000);
  end;

  if CurStep = ssPostInstall then
  begin
    ApiUrlVal := 'https://your-domain.com/api';
    ApiKeyVal := '';
    if ApiPage <> nil then
    begin
      if Length(ApiPage.Values[0]) > 0 then
        ApiUrlVal := ApiPage.Values[0];
      ApiKeyVal := ApiPage.Values[1];
    end;

    ConfigPath := ExpandConstant('{commonappdata}\PrintMonitor\config.json');

    // Create JSON configuration file if it does not exist
    if not FileExists(ConfigPath) then
    begin
      ConfigContent := '{'#13#10 +
        '  "ApiBaseUrl": "' + ApiUrlVal + '",'#13#10 +
        '  "ApiKey": "' + ApiKeyVal + '",'#13#10 +
        '  "SyncIntervalSeconds": 30,'#13#10 +
        '  "HeartbeatIntervalSeconds": 60,'#13#10 +
        '  "PollingIntervalSeconds": 5,'#13#10 +
        '  "DatabasePath": "C:\\ProgramData\\PrintMonitor\\printmonitor.db",'#13#10 +
        '  "LogDirectory": "C:\\ProgramData\\PrintMonitor\\Logs",'#13#10 +
        '  "LogLevel": "Information"'#13#10 +
        '}';

      SaveStringToFile(ConfigPath, ConfigContent, False);
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  DataDir: String;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    DataDir := ExpandConstant('{commonappdata}\PrintMonitor');
    if DirExists(DataDir) then
    begin
      if MsgBox('Do you want to permanently delete local print history and databases in ' + DataDir + '?' + #13#10 +
                'Select "No" to preserve your database and logs for future installations.',
                mbConfirmation, MB_YESNO or MB_DEFBUTTON2) = IDYES then
      begin
        DelTree(DataDir, True, True, True);
      end;
    end;
  end;
end;
