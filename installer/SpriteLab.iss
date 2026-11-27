#ifndef AppVersion
  #define AppVersion "0.6.8"
#endif
#ifndef PublishDir
  #define PublishDir "..\portable"
#endif
[Setup]
AppId={{B637E3EE-3A57-474F-9F87-C70E3F12AE3B}
AppName=SpriteLab
AppVersion={#AppVersion}
AppPublisher=Farga Studios
AppPublisherURL=https://github.com/Farga-Studios/SpriteLab
DefaultDirName={localappdata}\Programs\SpriteLab
DefaultGroupName=SpriteLab
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\dist
OutputBaseFilename=SpriteLab-{#AppVersion}-win-x64-setup
SetupIconFile=..\src\SpriteLab.App\Assets\SpriteLab.ico
UninstallDisplayIcon={app}\SpriteLab.App.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
DisableProgramGroupPage=yes
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{group}\SpriteLab"; Filename: "{app}\SpriteLab.App.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\SpriteLab"; Filename: "{app}\SpriteLab.App.exe"; Tasks: desktopicon; WorkingDir: "{app}"
[Run]
Filename: "{app}\SpriteLab.App.exe"; Description: "{cm:LaunchProgram,SpriteLab}"; Flags: nowait postinstall skipifsilent

[Code]
const
  SYNCHRONIZE = $00100000;
  WAIT_OBJECT_0 = 0;
  ERROR_INVALID_PARAMETER = 87;

function OpenProcess(dwDesiredAccess: LongWord; bInheritHandle: Boolean; dwProcessId: LongWord): THandle;
  external 'OpenProcess@kernel32.dll stdcall';
function WaitForSingleObject(hHandle: THandle; dwMilliseconds: LongWord): LongWord;
  external 'WaitForSingleObject@kernel32.dll stdcall';
function CloseHandle(hObject: THandle): Boolean;
  external 'CloseHandle@kernel32.dll stdcall';
function GetLastError(): LongWord;
  external 'GetLastError@kernel32.dll stdcall';

function InitializeSetup(): Boolean;
var
  PidText: String;
  Pid: Integer;
  Handle: THandle;
begin
  Result := True;
  PidText := ExpandConstant('{param:WAITPID|0}');
  Pid := StrToIntDef(PidText, -1);
  if Pid = 0 then Exit;
  if Pid < 0 then begin Result := False; Exit; end;
  Handle := OpenProcess(SYNCHRONIZE, False, Pid);
  if Handle = 0 then begin
    Result := GetLastError() = ERROR_INVALID_PARAMETER;
    Exit;
  end;
  try
    Result := WaitForSingleObject(Handle, 30000) = WAIT_OBJECT_0;
  finally
    CloseHandle(Handle);
  end;
end;
