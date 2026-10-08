; Script generated for Nexrein Printer Monitor Windows Suite
#define MyAppName "Nexrein Printer Monitor"
#define MyAppVersion "2.0.0"
#define MyAppPublisher "Nexrein"
#define MyAppURL "https://printmonitor.nexreindigital.co.ke"
#define MyAppExeName "PrintMonitor.exe"
#define MyManagerExeName "PrintMonitor.Manager.exe"
#define SourcePublishDir "..\src\PrintMonitor\bin\Release\net10.0\win-x64\publish"

[Setup]
AppId={{9F7B2B16-6F80-4D2A-B981-12B64F8B388D}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\PrintMonitor
DefaultGroupName=Nexrein Printer Monitor
LicenseFile=LICENSE.txt
OutputDir=..\release
OutputBaseFilename=PrintMonitor-Setup
SetupIconFile=assets\app.ico
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
Name: "autostart"; Description: "Automatically launch Nexrein Printer Monitor on Windows startup"; GroupDescription: "Windows Startup:"

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "NexreinPrinterMonitorManager"; ValueData: """{app}\{#MyManagerExeName}"""; Tasks: autostart; Flags: uninsdeletevalue

[Dirs]
Name: "{commonappdata}\PrintMonitor"; Permissions: authusers-modify
Name: "{commonappdata}\PrintMonitor\Logs"; Permissions: authusers-modify

[Files]
Source: "{#SourcePublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "assets\app.ico"; DestDir: "{app}"; Flags: ignoreversion
Source: "assets\app.png"; DestDir: "{app}"; Flags: ignoreversion

[InstallDelete]
Type: files; Name: "{commondesktop}\PrintMonitor Manager.lnk"
Type: files; Name: "{userdesktop}\PrintMonitor Manager.lnk"
Type: files; Name: "{commondesktop}\Nexrein Print Monitor.lnk"
Type: files; Name: "{userdesktop}\Nexrein Print Monitor.lnk"
Type: files; Name: "{userdesktop}\Nexrein Printer Monitor.lnk"

[Icons]
Name: "{group}\Nexrein Printer Monitor Control Panel"; Filename: "{app}\{#MyManagerExeName}"; IconFilename: "{app}\app.ico"
Name: "{group}\Nexrein Printer Monitor Status (CLI)"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--status"
Name: "{group}\Nexrein Printer Monitor Printers (CLI)"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--printers"
Name: "{group}\Uninstall Nexrein Printer Monitor"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Nexrein Printer Monitor"; Filename: "{app}\{#MyManagerExeName}"; Tasks: desktopicon; IconFilename: "{app}\app.ico"

[Run]
; 1. Register or update Service with sc.exe
Filename: "{sys}\sc.exe"; Parameters: "create ""PrintMonitor"" binPath= ""\""{app}\{#MyAppExeName}\"""" start= auto DisplayName= ""Nexrein Printer Monitor - Print Monitoring Agent"""; Flags: runhidden waituntilterminated; StatusMsg: "Registering Windows Service..."
Filename: "{sys}\sc.exe"; Parameters: "config ""PrintMonitor"" binPath= ""\""{app}\{#MyAppExeName}\"""" start= auto DisplayName= ""Nexrein Printer Monitor - Print Monitoring Agent"""; Flags: runhidden waituntilterminated
; 2. Set Description
Filename: "{sys}\sc.exe"; Parameters: "description ""PrintMonitor"" ""Monitors Windows print jobs and synchronizes print activity with the Nexrein Printer Monitor management system."""; Flags: runhidden waituntilterminated
; 3. Configure Recovery
Filename: "{sys}\sc.exe"; Parameters: "failure ""PrintMonitor"" reset= 86400 actions= restart/60000/restart/60000/restart/60000"; Flags: runhidden waituntilterminated
; 4. Start Service
Filename: "{sys}\sc.exe"; Parameters: "start ""PrintMonitor"""; Flags: runhidden waituntilterminated; StatusMsg: "Starting Nexrein Printer Monitor Service..."
; 5. Launch Manager GUI (optional postinstall)
Filename: "{app}\{#MyManagerExeName}"; Description: "Launch Nexrein Printer Monitor Control Panel"; Flags: postinstall nowait skipifsilent

[UninstallRun]
; Stop and delete service before removing files
Filename: "{sys}\sc.exe"; Parameters: "stop ""PrintMonitor"""; Flags: runhidden waituntilterminated; RunOnceId: "StopPrintMonitorService"
Filename: "{sys}\sc.exe"; Parameters: "delete ""PrintMonitor"""; Flags: runhidden waituntilterminated; RunOnceId: "DeletePrintMonitorService"

#define HardcodedApiUrl "https://printmonitor.nexreindigital.co.ke/api"

[Code]
var
  AccountPage: TInputQueryWizardPage;
  UserEmailInput: String;
  ShopNameInput: String;

procedure InitializeWizard;
begin
  AccountPage := CreateInputQueryPage(wpSelectDir,
    'Nexrein Cloud Account Registration',
    'Link this workstation with your Nexrein Printer Monitor online dashboard',
    'Please specify your registered account credentials below.'#13#10 +
    'The email address ties this PC to your cloud account at https://printmonitor.nexreindigital.co.ke.'#13#10 +
    'The computer/shop name identifies this station in your multi-printer fleet reports.');

  AccountPage.Add('Registered User Email Address:', False);
  AccountPage.Add('Computer / Shop Name:', False);

  AccountPage.Values[0] := 'admin@nexreindigital.co.ke';
  AccountPage.Values[1] := ExpandConstant('{computername}');
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  if (AccountPage <> nil) and (PageID = AccountPage.ID) then
  begin
    if WizardSilent then
    begin
      if UserEmailInput = '' then UserEmailInput := 'admin@nexreindigital.co.ke';
      if ShopNameInput = '' then ShopNameInput := ExpandConstant('{computername}');
      Result := True;
    end;
  end;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;
  if WizardSilent then
  begin
    if UserEmailInput = '' then UserEmailInput := 'admin@nexreindigital.co.ke';
    if ShopNameInput = '' then ShopNameInput := ExpandConstant('{computername}');
    Exit;
  end;

  if (AccountPage <> nil) and (CurPageID = AccountPage.ID) then
  begin
    if Trim(AccountPage.Values[0]) = '' then
    begin
      MsgBox('Please enter a valid user email address.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    if Trim(AccountPage.Values[1]) = '' then
    begin
      MsgBox('Please enter a computer or shop name.', mbError, MB_OK);
      Result := False;
      Exit;
    end;
    UserEmailInput := Trim(AccountPage.Values[0]);
    ShopNameInput := Trim(AccountPage.Values[1]);
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
var
  DisplayEmail: String;
begin
  if CurPageID = wpFinished then
  begin
    DisplayEmail := UserEmailInput;
    if DisplayEmail = '' then
      DisplayEmail := 'admin@nexreindigital.co.ke';

    WizardForm.FinishedLabel.Caption :=
      'Nexrein Printer Monitor has been successfully installed!'#13#10#13#10 +
      'ONLINE DASHBOARD ACCESS INSTRUCTIONS:'#13#10 +
      '1. Open your browser and navigate to:'#13#10 +
      '   https://printmonitor.nexreindigital.co.ke'#13#10#13#10 +
      '2. Log in using your email: ' + DisplayEmail + #13#10 +
      '   Default Password: admin'#13#10#13#10 +
      '3. Mandatory First Login Setup:'#13#10 +
      '   You will be asked to immediately change the default password to your own secure password.'#13#10#13#10 +
      'Click Finish to launch the Nexrein Printer Monitor Control Panel.';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ConfigPath: String;
  ConfigContent: String;
  ExistingConfig: AnsiString;
  ExistingStr: String;
  DbPath: String;
  DbBackupPath: String;
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    // Stop running manager and service cleanly before replacing binaries
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM PrintMonitor.Manager.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM PrintMonitor.exe', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec(ExpandConstant('{sys}\sc.exe'), 'stop "PrintMonitor"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1000);

    // Safeguard existing print database on version upgrades: create backup
    DbPath := ExpandConstant('{commonappdata}\PrintMonitor\printmonitor.db');
    DbBackupPath := ExpandConstant('{commonappdata}\PrintMonitor\printmonitor.db.upgrade_backup');
    if FileExists(DbPath) then
    begin
      CopyFile(DbPath, DbBackupPath, False);
    end;
  end;

  if CurStep = ssPostInstall then
  begin
    if UserEmailInput = '' then UserEmailInput := 'admin@nexreindigital.co.ke';
    if ShopNameInput = '' then ShopNameInput := ExpandConstant('{computername}');

    ConfigPath := ExpandConstant('{commonappdata}\PrintMonitor\config.json');

    // Create JSON configuration file with hardcoded default URL ONLY IF it does not exist
    // This strictly preserves all existing user endpoints, tokens, and database configurations on update
    if not FileExists(ConfigPath) then
    begin
      ConfigContent := '{'#13#10 +
        '  "ApiBaseUrl": "{#HardcodedApiUrl}",'#13#10 +
        '  "ApiKey": "",'#13#10 +
        '  "UserEmail": "' + UserEmailInput + '",'#13#10 +
        '  "ShopName": "' + ShopNameInput + '",'#13#10 +
        '  "SyncIntervalSeconds": 30,'#13#10 +
        '  "HeartbeatIntervalSeconds": 60,'#13#10 +
        '  "PollingIntervalSeconds": 5,'#13#10 +
        '  "DatabasePath": "C:\\ProgramData\\PrintMonitor\\printmonitor.db",'#13#10 +
        '  "LogDirectory": "C:\\ProgramData\\PrintMonitor\\Logs",'#13#10 +
        '  "LogLevel": "Information"'#13#10 +
        '}';

      SaveStringToFile(ConfigPath, ConfigContent, False);
    end
    else
    begin
      // Update existing configuration: migrate legacy URLs and ensure UserEmail/ShopName are populated
      if LoadStringFromFile(ConfigPath, ExistingConfig) then
      begin
        ExistingStr := String(ExistingConfig);
        StringChange(ExistingStr, 'https://your-domain.com/api', '{#HardcodedApiUrl}');
        StringChange(ExistingStr, 'https://your-domain.com', '{#HardcodedApiUrl}');
        StringChange(ExistingStr, 'http://your-domain.com/api', '{#HardcodedApiUrl}');
        StringChange(ExistingStr, 'http://your-domain.com', '{#HardcodedApiUrl}');

        if Pos('"UserEmail"', ExistingStr) = 0 then
        begin
          // Safely inject before the last closing brace
          ExistingStr := Copy(ExistingStr, 1, Length(ExistingStr) - 1) +
            '  ,"UserEmail": "' + UserEmailInput + '",'#13#10 +
            '  "ShopName": "' + ShopNameInput + '"'#13#10 + '}';
        end;
        SaveStringToFile(ConfigPath, ExistingStr, False);
      end;
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
