@echo off
chcp 65001 >nul 2>&1
title 向北 Northbound - 游戏启动器
cd /d "%~dp0"

echo ========================================
echo    向北 Northbound - 游戏启动器
echo ========================================
echo.

:: 检查 Node.js 是否安装
where node >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未检测到 Node.js，请先安装 Node.js 18+
    echo 下载地址: https://nodejs.org/
    echo.
    pause
    exit /b 1
)

:: 检查依赖是否已安装
if not exist "node_modules\vite" (
    echo [初始化] 首次运行，正在安装依赖...
    call npm install
    if %errorlevel% neq 0 (
        echo [错误] 依赖安装失败，请检查网络连接后重试
        pause
        exit /b 1
    )
    echo [完成] 依赖安装成功
    echo.
)

echo [启动] 正在启动游戏服务器...
echo [提示] 浏览器将自动打开，如未打开请手动访问 http://localhost:5173
echo [提示] 按 Ctrl+C 可关闭服务器
echo.

:: 延迟打开浏览器（等待服务器就绪），作为 Vite open:true 的备份
start "" cmd /c "timeout /t 3 /nobreak >nul & start "" "http://localhost:5173""

:: 启动 Vite 开发服务器（vite.config.ts 已配置 open:true 会自动打开浏览器）
call npx vite --host

:: 服务器停止后
echo.
echo [信息] 服务器已停止，按任意键关闭窗口
pause >nul
