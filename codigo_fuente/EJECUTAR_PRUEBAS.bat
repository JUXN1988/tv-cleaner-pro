@echo off
REM ==========================================================================
REM  Pruebas automaticas contra un TV SIMULADO (no toca ningun TV real).
REM  Requiere Python instalado (el simulador fakeadb.py esta escrito en Python).
REM ==========================================================================
setlocal
cd /d "%~dp0"
where python >nul 2>nul || (echo Necesitas Python para el simulador: https://www.python.org/downloads/ & pause & exit /b 1)
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist pruebas\bin mkdir pruebas\bin

"%CSC%" /nologo /codepage:65001 /out:pruebas\bin\PruebasMotor.exe ^
  /r:System.Web.Extensions.dll /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll /r:System.Core.dll ^
  /resource:recursos\uad_lists.json.gz,uad_lists.json.gz ^
  src\Utilidades.cs src\BaseDatos.cs src\TablasPaquetes.cs src\Adb.cs src\Historial.cs src\Motor.cs ^
  src\Instalador.cs src\EscanerRed.cs pruebas\PruebasMotor.cs
if errorlevel 1 (echo ERROR al compilar las pruebas & pause & exit /b 1)

REM servicios de red falsos en 127.0.0.2-5 para probar el escaner
start "servidores de prueba" /min python pruebas\servidores_red.py 180
timeout /t 2 /nobreak >nul
set PRUEBA_RANGO_RED=127.0.0.1-6
chcp 65001 >nul
pruebas\bin\PruebasMotor.exe "%~dp0pruebas\fakeadb.bat"
echo.
pause
