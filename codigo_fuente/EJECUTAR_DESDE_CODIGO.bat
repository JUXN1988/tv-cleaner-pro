@echo off
REM ==========================================================================
REM  Compila el codigo ACTUAL de la carpeta src y abre el programa enseguida.
REM  Ideal para probar tus cambios. Resultado: bin\TV_Cleaner_Pro.exe
REM  (no toca el ..\TV_Cleaner_Pro.exe principal; para eso usa COMPILAR.bat)
REM ==========================================================================
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo No se encontro csc.exe. Activa ".NET Framework 4.8" en "Activar o desactivar caracteristicas de Windows".
  echo Mientras tanto se abre la version ya compilada de bin\
  if exist "bin\TV_Cleaner_Pro.exe" start "" "bin\TV_Cleaner_Pro.exe"
  pause
  exit /b 1
)
if not exist bin mkdir bin
echo Compilando el codigo de src\ ...
"%CSC%" /nologo /codepage:65001 /target:winexe /platform:anycpu /optimize+ ^
  /out:"bin\TV_Cleaner_Pro_nuevo.exe" ^
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
  echo *** ERROR al compilar: revisa archivo y linea en los mensajes de arriba. ***
  echo     Se mantiene la version anterior de bin\TV_Cleaner_Pro.exe
  pause
  exit /b 1
)
move /y "bin\TV_Cleaner_Pro_nuevo.exe" "bin\TV_Cleaner_Pro.exe" >nul
echo Abriendo TV Cleaner Pro...
start "" "bin\TV_Cleaner_Pro.exe"
