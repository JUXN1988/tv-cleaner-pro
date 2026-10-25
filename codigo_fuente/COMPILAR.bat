@echo off
REM ==========================================================================
REM  Compila TV Cleaner Pro con el compilador de C# que ya trae Windows.
REM  No necesitas instalar Visual Studio ni nada mas.
REM  Resultado: ..\TV_Cleaner_Pro.exe  (la version anterior queda como _anterior)
REM ==========================================================================
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo No se encontro csc.exe. Activa ".NET Framework 4.8" en "Activar o desactivar caracteristicas de Windows".
  pause
  exit /b 1
)
if exist "..\TV_Cleaner_Pro.exe" copy /y "..\TV_Cleaner_Pro.exe" "..\TV_Cleaner_Pro_anterior.exe" >nul

echo Compilando...
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ ^
  /out:"..\TV_Cleaner_Pro.exe" ^
  /win32icon:recursos\icon.ico /win32manifest:recursos\app.manifest ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Web.Extensions.dll ^
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Core.dll ^
  /resource:recursos\uad_lists.json.gz,uad_lists.json.gz ^
  /resource:recursos\adb\adb.exe,adb/adb.exe ^
  /resource:recursos\adb\AdbWinApi.dll,adb/AdbWinApi.dll ^
  /resource:recursos\adb\AdbWinUsbApi.dll,adb/AdbWinUsbApi.dll ^
  src\*.cs
if errorlevel 1 (
  echo.
  echo *** ERROR al compilar. Revisa los mensajes de arriba: indican archivo y linea. ***
  pause
  exit /b 1
)
echo.
echo Listo: ..\TV_Cleaner_Pro.exe
pause
