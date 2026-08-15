@echo off
REM ============================================================
REM  Push to GitHub (SIMPLE / stable version)
REM  Syntax intentionally minimal - no for /f, no nested if blocks.
REM  This file should NEVER "flash and close" on double click.
REM ============================================================
REM  Double-click this file (0-PUSH.bat).
REM  If your proxy port differs from 7890, change PROXY_PORT below.
REM ============================================================

title 0-PUSH.bat - Northbound to GitHub
cd /d "%~dp0"

echo.
echo ============================================================
echo   PUSH NORTHBOUND TO GITHUB
echo   Folder : %cd%
echo ============================================================
echo.

set PROXY_PORT=7890

REM --- Precheck 1 : can we run git?
echo [1] looking for git...
git --version
if ERRORLEVEL 1 goto NO_GIT

echo.
REM --- Precheck 2 : is this a repo?
echo [2] looking for .git folder...
if NOT exist "%cd%\.git" goto NO_GIT_FOLDER

echo.
echo [3] local latest commit:
git log -1 --oneline
if ERRORLEVEL 1 goto NO_COMMIT

echo.
REM --- Proxy setup
echo [4] check proxy port %PROXY_PORT% ...
set PROXY_ARG=
netstat -ano 2>nul | findstr /R /C:":%PROXY_PORT% .*LISTENING" >nul 2>&1
if ERRORLEVEL 1 goto NO_PROXY
echo     proxy found on port %PROXY_PORT% -> will use it.
set PROXY_ARG=-c http.proxy=http://127.0.0.1:%PROXY_PORT% -c https.proxy=http://127.0.0.1:%PROXY_PORT%
goto PROXY_DONE
:NO_PROXY
echo     proxy NOT found on %PROXY_PORT% -> will try direct connect.
:PROXY_DONE

echo.
REM --- Remote add
echo [5] set github remote...
git remote remove github 2>nul
git remote add github https://github.com/denniskong666/Northbound.git
git remote get-url github
if ERRORLEVEL 1 goto REMOTE_FAIL

echo.
REM --- Fetch
echo [6] fetch remote main ...
git %PROXY_ARG% fetch github main
if ERRORLEVEL 1 goto FETCH_FAIL
echo     fetch ok.

echo.
REM --- Merge if needed
echo [7] merge remote main if needed ...
git rev-parse --verify github/main >nul 2>&1
if ERRORLEVEL 1 goto NO_MERGE
git merge --no-edit --allow-unrelated-histories -X ours github/main
:NO_MERGE

echo.
REM --- Push
echo [8] PUSH master -> main ...
git %PROXY_ARG% push -u github master:main
if ERRORLEVEL 1 goto PUSH_FAIL

echo.
echo ============================================================
echo   PUSH OK
echo   Open : https://github.com/denniskong666/Northbound
echo ============================================================
goto FIN

:NO_GIT
echo.
echo [FATAL] 'git' command not found. Please install Git for Windows:
echo         https://git-scm.com/download/win
goto FIN

:NO_GIT_FOLDER
echo.
echo [FATAL] No .git folder here. This .bat MUST stay inside the
echo         Capstone project folder where git was initialized.
goto FIN

:NO_COMMIT
echo.
echo [FATAL] No commits. Run: git add -A  then  git commit -m "init"
goto FIN

:REMOTE_FAIL
echo.
echo [FATAL] git remote add failed (previous output above)
goto FIN

:FETCH_FAIL
echo.
echo [FATAL] Cannot reach github.com.
echo         1) Start Clash/V2Ray on port %PROXY_PORT%
echo         2) Or change PROXY_PORT near top of this file to your port
goto FIN

:PUSH_FAIL
echo.
echo [FATAL] Push failed.
echo   If you see "Password for 'https://...'":
echo     Enter a GitHub Personal Access Token (NOT your login pw).
echo     Make one here: https://github.com/settings/tokens  (check repo)
echo     Then paste into this window and press Enter.
echo   If "fatal: repository not found":
echo     Make sure your GitHub account owns denniskong666/Northbound.
goto FIN

:FIN
echo.
echo ------------------------------------------------------------
echo  Done. Press any key to close this window.
pause >nul
