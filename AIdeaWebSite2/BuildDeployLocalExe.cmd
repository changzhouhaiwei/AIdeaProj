@echo off
chcp 65001 >nul
cd /d "%~dp0DeployLocalLauncher"

where dotnet >nul 2>&1
if errorlevel 1 (
  echo 未找到 dotnet，请安装 .NET 9 SDK 后再运行本脚本。
  pause
  exit /b 1
)

dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -o ".."
if errorlevel 1 (
  echo 编译失败。
  pause
  exit /b 1
)

echo.
echo 已生成 ..\DeployLocal.exe （自包含，体积较大）。请与 DeployLocal.cmd、Site 放在同一目录使用。
pause
