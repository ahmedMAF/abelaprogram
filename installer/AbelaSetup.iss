; Abela Management System - Inno Setup installer script
; Builds Setup.exe from the self-contained win-x64 publish output.

#define MyAppName "Abela Management System"
#define MyAppNameAr "نظام إدارة أبيلا"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Abela"
#define MyAppExeName "Abela.exe"
#define MyPublishDir "..\publish\win-x64"

[Setup]
AppId={{B4B8E9C1-7B8C-4C1E-9B4A-ABE1A0000001}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\Abela-Desktop-Release
OutputBaseFilename=Abela-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile=..\src\Abela.App\abela.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
PrivilegesRequired=admin
DisableWelcomePage=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "إنشاء اختصار على سطح المكتب"; GroupDescription: "اختصارات إضافية:"

[Files]
Source: "{#MyPublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\إلغاء تثبيت {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "تشغيل {#MyAppName} الآن"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Application files only; user data in %AppData%\Abela is intentionally preserved.

[Code]
procedure InitializeWizard;
begin
  WizardForm.WelcomeLabel2.Caption :=
    'سيقوم هذا المعالج بتثبيت نظام إدارة أبيلا لإدارة المبيعات والمشتريات والمخزون.' + #13#10#13#10 +
    'بيانات النظام (قاعدة البيانات والنسخ الاحتياطية) تُحفظ في مجلد بيانات المستخدم ولن تتأثر عند التحديث أو إلغاء التثبيت.' + #13#10#13#10 +
    'انقر التالي للمتابعة.';
end;

function InitializeUninstall(): Boolean;
begin
  Result := MsgBox('سيؤدي هذا إلى إلغاء تثبيت نظام إدارة أبيلا. بيانات النظام (قاعدة البيانات والنسخ الاحتياطية) لن يتم حذفها وستبقى في مجلد AppData\Abela. هل تريد المتابعة؟',
    mbConfirmation, MB_YESNO) = IDYES;
end;
