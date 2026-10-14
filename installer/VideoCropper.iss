; Compile through scripts/build-setup.ps1 with Inno Setup 7.1+.
; Keep AppId stable across releases so Setup updates the existing installation.
[Setup]
AppId=electblake.VideoCropper
AppName=Video Cropper
AppVersion={#AppVersion}
AppPublisher=electblake
AppPublisherURL=https://github.com/electblake/VideoCropper
AppSupportURL=https://github.com/electblake/VideoCropper/issues
AppUpdatesURL=https://github.com/electblake/VideoCropper/releases
DefaultDirName={autopf}\VideoCropper
UsePreviousAppDir=yes
UsePreviousTasks=yes
DefaultGroupName=Video Cropper
DisableProgramGroupPage=yes
InfoBeforeFile=prerequisites.txt
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
OutputDir={#SetupOutput}
OutputBaseFilename=VideoCropper_{#AppVersion}_x64_Setup
SetupIconFile=..\VideoCropper\Assets\EncircledPIERound.ico
UninstallDisplayIcon={app}\VideoCropper.exe
WizardStyle=modern
Compression=lzma2
SolidCompression=yes
ArchiveExtraction=enhanced/nopassword
CloseApplications=yes
; Restart Manager prompts to close the running app before replacing its files.
CloseApplicationsFilter=*.*
RestartApplications=no

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"; Flags: unchecked

[Files]
; Only application files are embedded. Dependencies are downloaded by Setup.
Source: "{#PayloadDir}\VideoCropper.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Excludes: "VideoCropper.exe,Assets\*.exe,Assets\*.dll"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "https://download.visualstudio.microsoft.com/download/pr/73aabf2e-9532-4f68-99f7-3247081a619c/CC0FF0EB1DC3F5188AE6300FAEF32BF5BEEBA4BDD6E8E445A9184072096B713B/VC_redist.x64.exe"; DestDir: "{tmp}"; DestName: "vc-redist.exe"; ExternalSize: 25_635_768; Hash: "cc0ff0eb1dc3f5188ae6300faef32bf5beeba4bdd6e8e445a9184072096b713b"; Flags: external download ignoreversion deleteafterinstall
Source: "https://download.microsoft.com/download/5ab91d2a-50f7-4cc1-a769-534f6d3d45d9/1943d4b5-db8e-465e-bd87-1c356f523e33/dotnet-runtime-8.0.32-win-x64.exe"; DestDir: "{tmp}"; DestName: "dotnet-runtime.exe"; ExternalSize: 28_622_264; Hash: "4fbc22376fa60c8255946920fe0d1b93ee526b6956f8b9c9ce6743cf67e08b43"; Flags: external download ignoreversion deleteafterinstall
Source: "https://aka.ms/windowsappsdk/1.7/1.7.260224002/windowsappruntimeinstall-x64.exe"; DestDir: "{tmp}"; DestName: "windowsappruntimeinstall.exe"; ExternalSize: 71_147_080; Hash: "8de73b13a010c6aeb84040e5587a46d21b36decce0ccd582c346536cad63ae73"; Flags: external download ignoreversion deleteafterinstall
Source: "https://github.com/GyanD/codexffmpeg/releases/download/9.0.2/ffmpeg-9.0.2-essentials_build.7z"; DestDir: "{tmp}\ffmpeg"; DestName: "ffmpeg.7z"; ExternalSize: 329_221_735; Hash: "4705843ccaaf54257c16ad90f3e952ece33c17df964ecf7bfdbb0f49c7171077"; Flags: external download extractarchive ignoreversion recursesubdirs
Source: "{tmp}\ffmpeg\ffmpeg-9.0.2-essentials_build\bin\ffmpeg.exe"; DestDir: "{app}\Assets"; ExternalSize: 105_423_872; Flags: external ignoreversion
Source: "{tmp}\ffmpeg\ffmpeg-9.0.2-essentials_build\LICENSE"; DestDir: "{app}\Assets\FFmpeg"; ExternalSize: 35_147; Flags: external ignoreversion
Source: "{tmp}\ffmpeg\ffmpeg-9.0.2-essentials_build\README.txt"; DestDir: "{app}\Assets\FFmpeg"; ExternalSize: 41_577; Flags: external ignoreversion

[Icons]
Name: "{autoprograms}\Video Cropper"; Filename: "{app}\VideoCropper.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Video Cropper"; Filename: "{app}\VideoCropper.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{tmp}\vc-redist.exe"; Parameters: "/install /passive /norestart"; StatusMsg: "Installing Visual C++ runtime..."; Flags: waituntilterminated
Filename: "{tmp}\dotnet-runtime.exe"; Parameters: "/install /passive /norestart"; StatusMsg: "Installing .NET 8 runtime..."; Flags: waituntilterminated
Filename: "{tmp}\windowsappruntimeinstall.exe"; StatusMsg: "Installing Windows App Runtime 1.7..."; Flags: waituntilterminated logoutput
Filename: "{app}\VideoCropper.exe"; Description: "Launch Video Cropper"; Flags: nowait postinstall skipifsilent
