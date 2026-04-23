#define MyAppName "ARSADECK"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "ARSA"
#define MyAppExeName "WpfApp1.exe"
#define MyAppSourcePath "G:\Visual Studio\repos\WpfApp1\WpfApp1\publish\WpfApp1.exe"

[Setup]
AppId={{9E4A4C2D-6A52-4C4D-A7A2-8F9D0C8B11F1}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=G:\Visual Studio\repos\WpfApp1\WpfApp1\InstallerOutput
OutputBaseFilename=ARSADECK_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesInstallIn64BitMode=x64
SetupIconFile=G:\Visual Studio\repos\WpfApp1\WpfApp1\Assets\arsadeck.ico
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные задачи:"; Flags: unchecked

[Files]
Source: "{#MyAppSourcePath}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Запустить {#MyAppName}"; Flags: nowait postinstall skipifsilent