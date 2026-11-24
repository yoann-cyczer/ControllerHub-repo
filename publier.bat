@echo off
setlocal
chcp 65001 >nul
title ControllerHub - Publication

rem Chemin fixe : le script doit etre place dans le dossier qui contient ControllerHub\.
set "PROJDIR=%~dp0ControllerHub\"
set "CSPROJ=%PROJDIR%ControllerHub.csproj"
if not exist "%CSPROJ%" (
    echo Fichier projet introuvable : %CSPROJ%
    echo Placez publier.bat dans le dossier qui contient le sous-dossier ControllerHub.
    pause
    exit /b 1
)
set "OUT=%~dp0ControllerHub-exe"

echo Publication de ControllerHub en executable autonome...
echo.
dotnet publish "%CSPROJ%" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "%OUT%"
if errorlevel 1 (
    echo.
    echo Echec de la publication. Lisez les messages ci-dessus.
    pause
    exit /b 1
)

rem Copie le fichier de mappage pour pouvoir le modifier sans recompiler.
copy /Y "%PROJDIR%mappings.json" "%OUT%\" >nul

echo.
echo Termine : %OUT%\ControllerHub.exe
explorer "%OUT%"
pause
