#ifndef AppVersion
  #define AppVersion "0.6.7"
#endif
#ifndef PublishDir
  #define PublishDir "..\portable"
#endif
[Setup]
AppId={{B637E3EE-3A57-474F-9F87-C70E3F12AE3B}
AppName=SpriteLab
AppVersion={#AppVersion}
AppPublisher=ManubeX
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
