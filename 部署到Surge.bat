@echo off
chcp 65001 >nul
title 向北 Northbound - Surge 部署
cd /d "%~dp0"

echo ============================================
echo    向北 Northbound - 一键部署到 Surge
echo ============================================
echo.
echo 首次使用需要输入邮箱和密码创建 Surge 账号
echo 之后再次运行此脚本可直接更新部署
echo.
echo 部署完成后会生成网址: northbound-game.surge.sh
echo.

echo 正在构建生产版本...
call npm.cmd run build
if errorlevel 1 (
    echo.
    echo 构建失败！请检查错误信息。
    pause
    exit /b 1
)

echo.
echo 构建成功！开始部署到 Surge...
echo.
npx surge dist northbound-game.surge.sh

echo.
echo ============================================
echo 部署完成！
echo 你的游戏网址: http://northbound-game.surge.sh
echo ============================================
echo.
pause
