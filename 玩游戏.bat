@echo off
chcp 65001 >nul
title 向北 Northbound - 本地网页版
cd /d "%~dp0"

echo ============================================
echo    向北 Northbound - 本地网页版
echo ============================================
echo.

REM 检查 dist 目录是否存在
if NOT exist "%cd%\dist\index.html" (
    echo 首次运行，正在构建游戏...
    echo.
    call npm.cmd run build
    if ERRORLEVEL 1 (
        echo.
        echo [错误] 构建失败，请检查 Node.js 是否安装。
        pause
        exit /b 1
    )
    echo.
    echo 构建完成！正在启动服务器...
    echo.
)

echo 正在启动本地服务器，Edge 浏览器会自动打开...
echo.
echo ============================================
echo   在 Edge 地址栏输入: http://localhost:9527
echo   或者直接等 3 秒，Edge 会自动打开
echo ============================================
echo.
echo 关闭此窗口即停止运行。
echo.

REM 3秒后用 Edge 自动打开游戏页面
start /b cmd /c "timeout /t 3 /nobreak >nul && start msedge http://localhost:9527"

REM 启动 Vite 本地预览服务器（阻塞式，关窗口即停）
npx.cmd vite preview --host --port 9527 --strictPort
