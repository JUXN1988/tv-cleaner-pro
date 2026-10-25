@echo off
REM Actualiza la lista comunitaria de paquetes (ver actualizar_lista_uad.ps1).
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0actualizar_lista_uad.ps1"
pause
