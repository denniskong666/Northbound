@echo off
REM ============================================================
REM  Northbound DEV launcher (starts Vite dev server)
REM  Double-click to start. Browser auto-opens on localhost:5173.
REM  No Chinese characters in this file to avoid cmd.exe crash.
REM ============================================================
title Northbound Dev Server
cd /d "%~dp0"

echo.
echo ============================================================
echo   NORTHBOUND - Dev Start
echo ============================================================
echo.

REM --- Check Node.js ---
where node >nul 2>&1
if ERRORLEVEL 1 (
    echo [FATAL] Node.js NOT found on PATH.
    echo         Install from https://nodejs.org/ (v18+ LTS recommended)
    echo.
    pause
    exit /b 1
)
for /f "delims=" %%v in ('node --version 2^>nul') do echo   Node.js %%v OK

REM --- Auto-install dependencies if needed ---
if NOT exist "node_modules\vite" (
    echo.
    echo [1/2] First run - installing dependencies via npm...
    echo       This takes 1-5 minutes.
    echo.
    call npm.cmd install
    if ERRORLEVEL 1 (
        echo.
        echo [FATAL] npm install failed. Check internet connection.
        pause
        exit /b 1
    )
    echo   Dependencies installed OK.
)

REM --- Open browser after short delay ---
echo.
echo [2/2] Starting Vite dev server on port 5173...
echo       Browser will open in 3 seconds.
echo       You can also visit: http://localhost:5173
echo       Press Ctrl+C in THIS window to stop.
echo.
start "" cmd /c "timeout /t 3 /nobreak >nul & start msedge http://localhost:5173"

REM --- Start dev server ---
call npx.cmd vite --host

echo.
echo Server stopped. Press any key...
pause >nul
