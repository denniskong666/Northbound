@echo off
REM ============================================================
REM  Deploy Northbound to Surge - FIXED DOMAIN
REM  After deploy, anyone can play via:
REM      http://northbound-game.surge.sh
REM  First run: Surge asks for email + password to signup/login.
REM  No Chinese chars in file -> avoid cmd.exe encoding crash.
REM ============================================================
title Deploy Northbound to Surge (fixed domain)
cd /d "%~dp0"

echo.
echo ============================================================
echo   DEPLOY NORTHBOUND -> SURGE
echo   Target URL: http://northbound-game.surge.sh
echo ============================================================
echo.
echo First time you run this, Surge asks for email + password
echo to create an account. After that, deploy is 1-click.
echo.

REM --- Node check ---
where node >nul 2>&1
if ERRORLEVEL 1 (
    echo [FATAL] Node.js not found. Install from https://nodejs.org/
    pause
    exit /b 1
)

REM --- Build production version ---
echo [1/2] Building production version (tsc + vite build)...
call npm.cmd run build
if ERRORLEVEL 1 (
    echo.
    echo [FATAL] Build failed. See error output above.
    pause
    exit /b 1
)
echo   Build OK.

REM --- Deploy to surge with FIXED domain ---
echo.
echo [2/2] Deploying dist/ -> northbound-game.surge.sh
echo        If prompted, enter your Surge email, then password.
echo.
npx.cmd surge dist northbound-game.surge.sh
if ERRORLEVEL 1 (
    echo.
    echo [FATAL] Deploy failed.
    echo   - Is your internet connected via proxy? Turn Clash/V2Ray ON.
    echo   - Did Surge login/password fail? Re-run and re-enter.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   DEPLOY SUCCESS
echo   Share this URL with anyone:
echo   http://northbound-game.surge.sh
echo ============================================================
echo.
echo Opening Edge to confirm...
start msedge http://northbound-game.surge.sh

echo.
pause
