@echo off
chcp 65001 >nul
title 向北 Northbound - 推送到 GitHub
cd /d "%~dp0"

echo ============================================
echo    向北 Northbound - 推送到 GitHub
echo ============================================
echo.
echo 目标仓库: https://github.com/denniskong666/Northbound.git
echo.
echo 说明：本脚本直接推送本地 master 到远端 main 分支
echo      远端空仓库，允许强制覆盖，不会丢失本地代码
echo.

REM ====== 配置代理端口（如 Clash 端口不同，改这里） ======
set PROXY_PORT=7890
REM =====================================================

REM 临时为 git 设置代理（不写入全局配置，关闭窗口即失效）
echo 正在检测代理端口 %PROXY_PORT% ...
netstat -ano | findstr ":%PROXY_PORT% " | findstr LISTENING >nul 2>&1
if errorlevel 1 (
    echo.
    echo [警告] 代理端口 %PROXY_PORT% 未检测到监听。
    echo        如果 Clash/V2Ray 用的不是 7890，请编辑本脚本，修改 PROXY_PORT。
    echo.
    echo 继续尝试直连推送（可能失败）...
    echo.
    set USE_PROXY=0
) else (
    echo 代理端口 %PROXY_PORT% 可用，已启用代理。
    set USE_PROXY=1
    set HTTPS_PROXY=http://127.0.0.1:%PROXY_PORT%
    set HTTP_PROXY=http://127.0.0.1:%PROXY_PORT%
)

echo.
echo ====== 1. 确保 GitHub 远端已添加 ======
git remote remove github 2>nul
git remote add github https://github.com/denniskong666/Northbound.git
git remote get-url github

echo.
echo ====== 2. 查看本地提交 ======
git log -3 --oneline

echo.
echo ====== 3. 拉取远端 main 并合并（防止首次推送冲突） ======
if "%USE_PROXY%"=="1" (
    git -c http.proxy=%HTTP_PROXY% -c https.proxy=%HTTPS_PROXY% fetch github
) else (
    git fetch github
)
if errorlevel 1 (
    echo.
    echo [错误] fetch 失败，无法连接 GitHub。
    echo        请检查代理软件是否开启，或 PROXY_PORT 是否正确。
    pause
    exit /b 1
)

echo.
echo ====== 4. 推送 ======
set DST_BRANCH=main
set SRC_BRANCH=master
if "%USE_PROXY%"=="1" (
    git -c http.proxy=%HTTP_PROXY% -c https.proxy=%HTTPS_PROXY% push -u github %SRC_BRANCH%:%DST_BRANCH%
) else (
    git push -u github %SRC_BRANCH%:%DST_BRANCH%
)
if errorlevel 1 (
    echo.
    echo [错误] push 失败。
    echo 若提示 authentication 错误，请改用 GitHub Token：
    echo   1. 打开 https://github.com/settings/tokens 生成一个勾选 repo 的 Token
    echo   2. 当提示输入密码时，粘贴 Token（不显示是正常的）然后回车
    echo 或改用 SSH remote：
    echo   git remote set-url github git@github.com:denniskong666/Northbound.git
    echo.
    pause
    exit /b 1
)

echo.
echo ============================================
echo  推送成功！
echo  打开查看: https://github.com/denniskong666/Northbound
echo ============================================
echo.
pause
