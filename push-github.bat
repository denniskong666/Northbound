@echo off
REM ============================================================
REM  Northbound -> GitHub push script
REM  Double-click to run. No Chinese chars to avoid encoding bugs.
REM ============================================================
REM Keep window open if double-clicked.
REM If something goes wrong we can always read the output.

setlocal
title Push Northbound to GitHub

cd /d "%~dp0"

cls
echo.
echo ============================================================
echo   PUSH NORTHBOUND -> GITHUB
echo   Repo : https://github.com/denniskong666/Northbound.git
echo   From : %cd%
echo ============================================================
echo.

REM ======= User config =========================================
set PROXY_PORT=7890
REM ============================================================

echo [PRE-CHECK 1/3] Is git on PATH?
where git >nul 2>&1
if errorlevel 1 (
    echo   [FATAL] 'git' is NOT found.
    echo   Install Git for Windows from https://git-scm.com/download/win
    echo   Then RE-OPEN this script.
    goto END
)
for /f "delims=" %%v in ('git --version 2^>nul') do echo   OK : %%v

echo.
echo [PRE-CHECK 2/3] Is this a git repository?
if not exist "%cd%\.git" (
    echo   [FATAL] .git folder not found in: %cd%
    echo   This script must be placed and run inside the project folder
    echo   where the hidden .git folder lives.
    goto END
)
echo   OK : .git found.

echo.
echo [PRE-CHECK 3/3] Do we have at least 1 commit?
git rev-parse --verify HEAD >nul 2>&1
if errorlevel 1 (
    echo   [FATAL] No local commits yet.
    echo   Run:  git add -A ^&^& git commit -m "init"
    goto END
)
for /f "delims=" %%c in ('git log -1 --oneline') do echo   OK : local head = %%c

echo.
echo ------------------------------------------------------------
echo [1/6] Proxy detection on port %PROXY_PORT%
set USE_PROXY=0
netstat -ano 2>nul | findstr /R /C:":%PROXY_PORT% .*LISTENING" >nul 2>&1
if errorlevel 1 (
    echo   Proxy NOT listening on %PROXY_PORT%. Trying direct push.
    echo   (If you use Clash, turn it ON ^& verify port, then retry)
) else (
    echo   Proxy OK on port %PROXY_PORT%. Enabled for git HTTPS.
    set USE_PROXY=1
    set HTTP_PROXY=http://127.0.0.1:%PROXY_PORT%
    set HTTPS_PROXY=http://127.0.0.1:%PROXY_PORT%
)

echo.
echo ------------------------------------------------------------
echo [2/6] (Re)add github remote
git remote remove github 2>nul
git remote add github https://github.com/denniskong666/Northbound.git
git remote get-url github

echo.
echo ------------------------------------------------------------
echo [3/6] Fetch remote main
if "%USE_PROXY%"=="1" (
    git -c http.proxy=%HTTP_PROXY% -c https.proxy=%HTTPS_PROXY% fetch github main
) else (
    git fetch github main
)
if errorlevel 1 (
    echo.
    echo [FAIL] Could not reach github.com over HTTPS.
    echo   Fix: start your proxy (Clash/V2Ray...) on port %PROXY_PORT%,
    echo   or edit PROXY_PORT at the top of this file to match yours.
    goto END
)
echo   Fetch OK.

echo.
echo ------------------------------------------------------------
echo [4/6] Merge remote main into local master (only first time)
REM Only merge if remote main is non-empty ancestor-unrelated first push.
git rev-parse --verify github/main >nul 2>&1
if not errorlevel 1 (
    REM Remote main exists; try to merge; skip if already up to date
    git merge-base --is-ancestor github/main master >nul 2>&1
    if errorlevel 1 (
        echo   Merging remote main ^(allow unrelated histories, ours on conflict^)...
        git merge --no-edit -X ours --allow-unrelated-histories github/main
        if errorlevel 1 (
            echo   Merge had conflicts; auto-keeping local versions.
            git diff --name-only --diff-filter=U
            git checkout --theirs . 2>nul
            git checkout --ours . 2>nul
            for /f "delims=" %%f in ('git diff --name-only --diff-filter=U') do (
                git checkout --ours -- "%%f" 2>nul
                git add -- "%%f" 2>nul
            )
            git commit --no-edit 2>nul
        )
    ) else (
        echo   Remote main already ancestor of master; skip merge.
    )
) else (
    echo   Remote main is empty; no merge needed.
)

echo.
echo ------------------------------------------------------------
echo [5/6] Push local master -> remote main
if "%USE_PROXY%"=="1" (
    git -c http.proxy=%HTTP_PROXY% -c https.proxy=%HTTPS_PROXY% push -u github master:main
) else (
    git push -u github master:main
)
if errorlevel 1 (
    echo.
    echo [FAIL] push rejected.
    echo   Typical fixes:
    echo   1) If prompt says "Password": PASTE a GitHub Token here.
    echo      Make token: https://github.com/settings/tokens  (check REPO)
    echo      ^(paste into this window and press ENTER - it will not show stars^)
    echo   2) Or use SSH: git remote set-url github git@github.com:denniskong666/Northbound.git
    goto END
)

echo.
echo ============================================================
echo   PUSH SUCCESS. Open the link below to verify:
echo   https://github.com/denniskong666/Northbound
echo ============================================================

:END
echo.
echo Script finished. Press any key to close this window...
pause >nul
endlocal
