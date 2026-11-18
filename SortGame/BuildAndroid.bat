@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BuildAndroid.ps1" %*
exit /b %ERRORLEVEL%
