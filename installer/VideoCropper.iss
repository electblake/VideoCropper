; Compile through scripts/build-setup.sh with Inno Setup 7.1+.
; Keep AppId stable across releases so Setup updates the existing installation.
[Setup]
AppId=electblake.VideoCropper
AppName=Video Cropper
AppVersion={#AppVersion}
AppPublisher=electblake
AppPublisherURL=https://github.com/electblake/VideoCropper
AppSupportURL=https://github.com/electblake/VideoCropper/issues
AppUpdatesURL=https://github.com/electblake/VideoCropper/releases
DefaultDirName={localappdata}\Programs\VideoCropper
DefaultGroupName=Video Cropper
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#SetupOutput}
OutputBaseFilename=VideoCropper_{#AppVersion}_x64_Setup
UninstallDisplayIcon={app}\VideoCropper.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
; The portable payload contains .NET, Windows App SDK, and FFmpeg dependencies.
Source: "{#PayloadDir}\VideoCropper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\Assets\ffmpeg.exe"; DestDir: "{app}\Assets"; Flags: ignoreversion
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "VideoCropper.exe,Assets\ffmpeg.exe"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\Video Cropper"; Filename: "{app}\VideoCropper.exe"; WorkingDir: "{app}"
Name: "{userdesktop}\Video Cropper"; Filename: "{app}\VideoCropper.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\VideoCropper.exe"; Description: "Launch Video Cropper"; Flags: nowait postinstall skipifsilent
