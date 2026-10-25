@echo off
REM ADB falso para pruebas: simula un dispositivo. No toca ningun TV real.
python "%~dp0fakeadb.py" %*
