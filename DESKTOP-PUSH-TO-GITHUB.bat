@echo off
REM ============================================================
REM  DESKTOP LAUNCHER - 双击直接推送到 GitHub
REM  =使用方法=
REM  1) 把这个 .bat 放在桌面（或者任何地方都行，它会自动找到项目）
REM  2) 双击运行（窗口不会自动关，任何错误都停留在屏幕上）
REM  =编码=
REM  全文件无中文字符，避免 cmd 解析期闪退。
REM ============================================================

title DESKTOP LAUNCHER - push Northbound to GitHub

REM --- 下面一行务必是你本机的 Capstone 项目绝对路径 ---
set PROJECT_DIR=C:\Users\lenovo\Desktop\Capstone
REM -------------------------------------------------------

if NOT exist "%PROJECT_DIR%\0-PUSH.bat" (
    echo.
    echo [FATAL] Cannot find project at:
    echo         %PROJECT_DIR%
    echo         Edit this file and fix PROJECT_DIR to the folder
    echo         where 0-PUSH.bat lives.
    echo.
    echo Press any key to close...
    pause >nul
    exit /b 1
)

cd /d "%PROJECT_DIR%"
echo Launched from desktop. Running inside project: %cd%
echo.
call "%PROJECT_DIR%\0-PUSH.bat"
