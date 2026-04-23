[Setup]
AppId={{A1E4D58B-7E5D-4E1C-A5B1-ARSADECK001}}
AppName=ARSADECK
AppVersion=1.0
AppPublisher=ARSA
DefaultDirName={autopf}\ARSADECK
DefaultGroupName=ARSADECK
UninstallDisplayIcon={app}\WpfApp1.exe
SetupIconFile=I:\Visual Studio\repos\WpfApp1\WpfApp1\Assets\arsadeck.ico
OutputDir=I:\Visual Studio\repos\WpfApp1\Output
OutputBaseFilename=ARSADECK_Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "Ярлык на рабочем столе"; GroupDescription: "Дополнительно:"

[Files]
Source: "I:\Visual Studio\repos\WpfApp1\publish\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion; Excludes: "*.pdb"

[Icons]
Name: "{group}\ARSADECK"; Filename: "{app}\WpfApp1.exe"; IconFilename: "{app}\WpfApp1.exe"
Name: "{autodesktop}\ARSADECK"; Filename: "{app}\WpfApp1.exe"; IconFilename: "{app}\WpfApp1.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\WpfApp1.exe"; Description: "Запустить ARSADECK"; Flags: nowait postinstall skipifsilent