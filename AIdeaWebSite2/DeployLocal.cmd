@echo off
chcp 65001 >nul
cd /d "%~dp0"

where node >nul 2>&1
if errorlevel 1 (
  echo [DeployLocal] 未找到 node，请先安装 Node.js LTS。
  pause
  exit /b 1
)
where npm >nul 2>&1
if errorlevel 1 (
  echo [DeployLocal] 未找到 npm，请检查 Node.js 安装是否完整。
  pause
  exit /b 1
)

node "%~dp0Site\scripts\deploy-local-entry.cjs"
if errorlevel 1 (
  echo.
  echo [DeployLocal] 执行出错，见上方日志。
  pause
  exit /b 1
)

echo.
echo [DeployLocal] 已尝试在浏览器打开站点。预览在单独窗口运行，关闭该窗口即停止服务。
pause
