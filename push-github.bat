@echo off
REM ============================================================
REM  Northbound - push to GitHub (denniskong666/Northbound)
REM  Double-click to run. If proxy port is not 7890, edit PROXY_PORT below.
REM ============================================================
setlocal

cd /d "%~dp0"
echo.
echo ============================================================
echo   Northbound -> GitHub push
echo   Target: https://github.com/denniskong666/Northbound.git
echo   Current dir: %cd%
echo ============================================================
echo.

REM ======= EDIT THIS IF YOUR PROXY USES A DIFFERENT PORT =======
set PROXY_PORT=7890
REM ============================================================

echo [1/5] Check proxy port %PROXY_PORT% ...
netstat -ano 2>nul | findstr /R /C:":%PROXY_PORT% .*LISTENING" >nul 2>&1
if errorlevel 1 (
    echo       Proxy NOT detected on %PROXY_PORT%.
    echo       If Clash/V2Ray uses another port, edit PROXY_PORT in this file.
    echo       Will try direct connect (may fail in China).
    set USE_PROXY=0
) else (
    echo       Proxy OK on port %PROXY_PORT%.
    set USE_PROXY=1
    set HTTP_PROXY=http://127.0.0.1:%PROXY_PORT%
    set HTTPS_PROXY=http://127.0.0.1:%PROXY_PORT%
)

echo.
echo [2/5] Ensure GitHub remote ...
git remote remove github 2>nul
git remote add github https://github.com/denniskong666/Northbound.git
if errorlevel 1 (
    echo [ERROR] git remote add failed. Is git installed?
    pause
    exit /b 1
)
git remote get-url github

echo.
echo [3/5] Local last commit:
git log -1 --oneline

echo.
echo [4/5] Fetch remote main (avoid branch conflict) ...
if "%USE_PROXY%"=="1" (
    git -c http.proxy=%HTTP_PROXY% -c https.proxy=%HTTPS_PROXY% fetch github
) else (
    git fetch github
)
if errorlevel 1 (
    echo.
    echo [ERROR] Failed to fetch github.
    echo   - Turn ON your proxy (Clash/V2Ray), or
    echo   - Fix PROXY_PORT in this batch file, then try again.
    pause
    exit /b 1
)

echo.
echo [5/5] Push local master -> remote main ...
if "%USE_PROXY%"=="1" (
    git -c http.proxy=%HTTP_PROXY% -c https.proxy=%HTTPS_PROXY% push -u github master:main
) else (
    git push -u github master:main
)
if errorlevel 1 (
    echo.
    echo [ERROR] push failed.
    echo   If password is asked, enter your GitHub Personal Access Token:
    echo   https://github.com/settings/tokens  (check "repo" scope)
    echo   Or switch to SSH: git remote set-url github git@github.com:denniskong666/Northbound.git
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   SUCCESS! Code pushed to GitHub.
echo   Open: https://github.com/denniskong666/Northbound
echo ============================================================
echo.
pause
endlocal
