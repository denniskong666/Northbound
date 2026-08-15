@echo off
chcp 65001 >nul
title 向北 Northbound - Surge 部署
cd /d "%~dp0"

echo ============================================
echo    向北 Northbound - 一键部署到 Surge
echo ============================================
echo.

echo 正在构建生产版本...
call npm.cmd run build
if errorlevel 1 (
    echo 构建失败！
    pause
    exit /b 1
)

echo.
echo 构建成功！正在部署...
echo.

set RANDOM_SUFFIX=%RANDOM%%RANDOM%
set DOMAIN=northbound-game-%RANDOM_SUFFIX%.surge.sh

echo 目标域名: %DOMAIN%
echo.

npx surge dist %DOMAIN%

echo.
if errorlevel 1 (
    echo 部署失败，请检查网络或邮箱密码。
) else (
    echo ============================================
    echo 部署成功！
    echo 你的游戏网址: http://%DOMAIN%
    echo ============================================
)
echo.
pause
