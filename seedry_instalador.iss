; ============================================================
;  Seedry — Script Inno Setup
;  Download gratuito: https://jrsoftware.org/isinfo.php
;
;  Como usar:
;    1. Rode publicar.bat primeiro para gerar a pasta publish\
;    2. Abra este arquivo no Inno Setup Compiler
;    3. Build > Compile  (ou Ctrl+F9)
;    4. O instalador será gerado em Output\Seedry_Setup_1.0.0.exe
; ============================================================

#define AppName        "Seedry"
#define AppVersion     "1.0.0"
#define AppPublisher   "Seedry"
#define AppURL         "https://seedry.com.br"
#define AppExeName     "SiloManager.WPF.exe"
#define AppDescription "Gestão de Umidade em Silos"

; GUID único para este instalador — NÃO altere após a primeira instalação
; (garante que o desinstalador encontre versões anteriores)
#define AppGuid "{A3F2B1C4-8E7D-4A2F-9B5C-1D3E6F0A7B8C}"

[Setup]
AppId={{#AppGuid}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
AppCopyright=Copyright © 2026 {#AppPublisher}
AppComments={#AppDescription}

; Pasta de instalação padrão: C:\Program Files\Seedry
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; Permite instalar sem ser administrador (recomendado para app por usuário)
; Comente a linha abaixo para forçar instalação global (requer admin)
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog

; Arquivo de saída
OutputDir=Output
OutputBaseFilename=Seedry_Setup_{#AppVersion}
SetupIconFile=

; Compressão máxima
Compression=lzma2/ultra64
SolidCompression=yes
LZMAUseSeparateProcess=yes

; Aparência
WizardStyle=modern
WizardSizePercent=120
DisableWelcomePage=no
DisableReadyPage=no

; Não reiniciar após instalação
RestartIfNeededByRun=no

; Metadados do instalador
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription=Instalador do {#AppName}
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "portuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon";     Description: "Criar ícone na Área de Trabalho"; GroupDescription: "Atalhos:"; Flags: unchecked
Name: "quicklaunchicon"; Description: "Criar ícone na barra de tarefas";                  GroupDescription: "Atalhos:"; Flags: unchecked; OnlyBelowVersion: 6.1

[Files]
; Executável principal e todos os arquivos da pasta publish
Source: "publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\*";             DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "{#AppExeName}"

; ── Driver USB-Serial (CP210x — Gehaka G810-I) ──────────────────────────
; Descomente as linhas abaixo se quiser incluir o driver no instalador.
; Coloque o instalador do driver na pasta "drivers\" ao lado deste .iss.
;
; Source: "drivers\CP210xVCPInstaller_x64.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall
; Source: "drivers\CP210xVCPInstaller_x86.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall

[Icons]
; Menu Iniciar
Name: "{group}\{#AppName}";           Filename: "{app}\{#AppExeName}"; Comment: "{#AppDescription}"
Name: "{group}\Desinstalar {#AppName}"; Filename: "{uninstallexe}"

; Área de trabalho (apenas se a task foi marcada)
Name: "{autodesktop}\{#AppName}";     Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
; Abre o aplicativo após instalar (opcional)
Filename: "{app}\{#AppExeName}"; Description: "Iniciar o {#AppName} agora"; Flags: nowait postinstall skipifsilent

; ── Driver USB-Serial (descomente junto com o bloco [Files] acima) ───────
; Filename: "{tmp}\CP210xVCPInstaller_x64.exe"; Parameters: "/silent"; StatusMsg: "Instalando driver USB-Serial..."; Flags: waituntilterminated; Check: IsWin64
; Filename: "{tmp}\CP210xVCPInstaller_x86.exe"; Parameters: "/silent"; StatusMsg: "Instalando driver USB-Serial..."; Flags: waituntilterminated; Check: not IsWin64

[UninstallDelete]
; Remove a pasta inteira ao desinstalar (inclusive o banco SQLite fica em AppData, não aqui)
Type: filesandordirs; Name: "{app}"

[Code]
// Verifica se já existe uma versão instalada e oferece desinstalar antes
function InitializeSetup(): Boolean;
var
  UninstallString: String;
  ResultCode: Integer;
begin
  Result := True;

  if RegQueryStringValue(HKLM, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppGuid}_is1',
    'UninstallString', UninstallString) or
    RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppGuid}_is1',
    'UninstallString', UninstallString) then
  begin
    if MsgBox('Uma versão anterior do ' + '{#AppName}' + ' foi encontrada.' + #13#10 +
              'Deseja desinstalar antes de continuar?',
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      Exec(RemoveQuotes(UninstallString), '/SILENT', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
    end;
  end;
end;
