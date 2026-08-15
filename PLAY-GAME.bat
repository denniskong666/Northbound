@echo off
REM ============================================================
REM  One-click to play Northbound in Edge browser
REM  Double-click this file. Local server starts, then Edge
REM  opens automatically after 3 seconds.
REM ============================================================
title Play Northbound
cd /d "%~dp0"

echo.
echo ============================================================
echo   PLAY NORTHBOUND - Local web server
echo ============================================================
echo.

REM ---- 1. Build if not yet built ----
if NOT exist "%cd%\dist\index.html" (
    echo First run detected - building the game...
    echo.
    call npm.cmd run build
    if ERRORLEVEL 1 (
        echo.
        echo [FATAL] Build failed.
        echo Did you install Node.js from https://nodejs.org ?
        pause
        exit /b 1
    )
    echo.
    echo Build finished. Starting server...
    echo.
)

REM ---- 2. Start server ----
echo Edge will open the game in 3 seconds.
echo You can also type this URL into Edge manually:
echo.
echo     http://localhost:9527
echo.
echo Close THIS window to stop the server.
echo.

REM Open Edge after a short delay so server is ready
start "" cmd /c "timeout /t 3 /nobreak >nul & start msedge http://localhost:9527"

REM Run vite preview (blocking - keeps the window alive)
npx.cmd vite preview --host --port 9527 --strictPort

echo.
echo Server stopped. Press any key...
pause >nul
