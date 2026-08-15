@echo off
REM ============================================================
REM  Deploy Northbound to Surge - RANDOM DOMAIN (no conflict)
REM  Use this if fixed domain northbound-game.surge.sh is already
REM  taken by another Surge account. Generates random suffix.
REM  No Chinese chars -> no encoding crash.
REM ============================================================
title Deploy Northbound to Surge (random domain)
cd /d "%~dp0"

echo.
echo ============================================================
echo   DEPLOY NORTHBOUND -> SURGE (random domain)
echo ============================================================
echo.

REM --- Node check ---
where node >nul 2>&1
if ERRORLEVEL 1 (
    echo [FATAL] Node.js not found. Install from https://nodejs.org/
    pause
    exit /b 1
)

REM --- Build ---
echo [1/2] Building production version...
call npm.cmd run build
if ERRORLEVEL 1 (
    echo.
    echo [FATAL] Build failed.
    pause
    exit /b 1
)
echo   Build OK.

REM --- Pick random domain ---
set RAND1=%RANDOM%
set RAND2=%RANDOM%
set DOMAIN=northbound-game-%RAND1%%RAND2%.surge.sh

echo.
echo [2/2] Deploying to: http://%DOMAIN%
echo        If prompted, enter Surge email + password.
echo.
npx.cmd surge dist %DOMAIN%
if ERRORLEVEL 1 (
    echo.
    echo [FATAL] Deploy failed. Check internet / proxy.
    pause
    exit /b 1
)

echo.
echo ============================================================
echo   DEPLOY SUCCESS
echo   Share this URL: http://%DOMAIN%
echo ============================================================
echo.
echo Opening Edge to confirm...
start msedge http://%DOMAIN%

echo.
pause
