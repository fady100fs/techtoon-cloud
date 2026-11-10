; =====================================================================
;  Techtoon Accounting — Inno Setup installer
;  يبني ملف تثبيت واحد (Setup.exe) يثبّت البرنامج على جهاز العميل.
;  Requirements: Inno Setup 6.x  (https://jrsoftware.org/isdl.php)
;  Build with:   deploy\installer\build-installer.ps1
; =====================================================================
#define AppName "Techtoon Accounting"
#define AppNameAr "تكتيون أكاونتينج"
#define AppVersion "1.0.0"
#define AppPublisher "Techtoon"
#define AppExeName "Techtoon.UI.exe"

[Setup]
AppId={{8F3C1D62-4A21-4B1E-9E77-6C9D2A5B4E10}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\TechtoonAccounting
DefaultGroupName={#AppName}
UninstallDisplayName={#AppName} {#AppVersion}
OutputBaseFilename=TechtoonSetup
OutputDir=..\..\dist
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
PrivilegesRequired=admin
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#AppExeName}
SetupIconFile=..\..\logo.ico
LicenseFile=..\..\LICENSE.txt

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"

[CustomMessages]
english.CreateDesktopIcon=Create a &desktop shortcut
english.LaunchApp=Launch {#AppName} after installation
english.LicenseInfo=This copy runs in trial mode (10 days). Place a license file named license.key next to the program to activate.
arabic.CreateDesktopIcon=اختر &اختصارًا على سطح المكتب
arabic.LaunchApp=تشغيل {#AppNameAr} بعد التثبيت
arabic.LicenseInfo=هذه النسخة تعمل في وضع تجريبي (10 أيام). ضع ملف الترخيص license.key بجانب البرنامج للتفعيل.

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; كل مخرجات النشر (ملف EXE واحد + ملفات旁边的ه)
Source: "..\..\dist\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; ملف ترخيص اختياري (إن وُجد) — يمكن نسخه لاحقًا بجانب البرنامج
Source: "..\..\dist\license.key"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist uninsneveruninstall

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\{#AppName} (Uninstall)"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchApp}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; ملفات مؤقتة للترخيص/السجل
Type: files; Name: "{app}\techtoon.log"

[Code]
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    if MsgBox(CustomMessage('LicenseInfo'), mbInformation, MB_OK) = IDOK then
      { لا شيء — رسالة informative فقط }
  end;
end;
