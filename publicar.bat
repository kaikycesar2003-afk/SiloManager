@echo off
:: ============================================================
::  Seedry — Script de publicação
::  Gera o executável self-contained em .\publish\
::  Requisito: .NET 10 SDK instalado
::    https://dotnet.microsoft.com/download/dotnet/10.0
:: ============================================================

echo.
echo  ========================================
echo   Seedry — Publicando para Windows x64
echo  ========================================
echo.

:: Vai para a raiz da solução (pasta onde está este .bat)
cd /d "%~dp0"

:: Limpa publicação anterior
if exist publish rmdir /s /q publish

:: Publica
dotnet publish SiloManager.WPF/SiloManager.WPF.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -p:DebugType=none ^
  -p:DebugSymbols=false ^
  -o ./publish

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo  [ERRO] Falha na publicacao. Verifique o log acima.
    pause
    exit /b 1
)

echo.
echo  ========================================
echo   Publicacao concluida com sucesso!
echo   Pasta: %~dp0publish\
echo  ========================================
echo.
echo  Proximo passo: abra seedry_instalador.iss
echo  no Inno Setup e clique em Build.
echo.
pause
