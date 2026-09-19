@echo off
chcp 65001 >nul
title HomeworkGate — Установка

echo.
echo  ╔══════════════════════════════════════╗
echo  ║     HomeworkGate — Установка v2.0    ║
echo  ╚══════════════════════════════════════╝
echo.

:: ── Шаг 1: Проверяем .NET 8 ──────────────────────────────────────────────────
echo [1/4] Проверка .NET 8...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo.
    echo  ❌ .NET 8 SDK не найден.
    echo     Открываю страницу загрузки...
    start https://dotnet.microsoft.com/download/dotnet/8
    echo.
    echo  Установи .NET 8 SDK и запусти этот файл снова.
    pause
    exit /b 1
)
for /f "tokens=1" %%v in ('dotnet --version 2^>nul') do set DOTNET_VER=%%v
echo     ✓ .NET %DOTNET_VER% найден

:: ── Шаг 2: Сборка ─────────────────────────────────────────────────────────────
echo.
echo [2/4] Сборка проекта...
dotnet build HomeworkGate.sln -c Release --nologo -v quiet
if errorlevel 1 (
    echo.
    echo  ❌ Ошибка сборки. Детали выше.
    pause
    exit /b 1
)
echo     ✓ Сборка успешна

:: ── Шаг 3: Копируем Guardian рядом с App ──────────────────────────────────────
echo.
echo [3/4] Копирование Guardian Service...
set APP_OUT=HomeworkGate.App\bin\Release\net8.0-windows
set GRD_OUT=HomeworkGate.Guardian\bin\Release\net8.0-windows\win-x64

if exist "%GRD_OUT%\HomeworkGate.Guardian.exe" (
    copy /Y "%GRD_OUT%\HomeworkGate.Guardian.exe" "%APP_OUT%\" >nul
    copy /Y "%GRD_OUT%\*.dll" "%APP_OUT%\" >nul 2>&1
    echo     ✓ Guardian скопирован
) else (
    echo     ⚠ Guardian.exe не найден, пропускаю (сервис можно установить позже из настроек)
)

:: ── Шаг 4: Создаём ярлык на рабочем столе ────────────────────────────────────
echo.
echo [4/4] Создание ярлыка на рабочем столе...
set EXE_PATH=%~dp0%APP_OUT%\HomeworkGate.exe
set SHORTCUT=%USERPROFILE%\Desktop\HomeworkGate.lnk

powershell -NoProfile -Command ^
    "$s=(New-Object -ComObject WScript.Shell).CreateShortcut('%SHORTCUT%');" ^
    "$s.TargetPath='%EXE_PATH%';" ^
    "$s.WorkingDirectory='%~dp0%APP_OUT%';" ^
    "$s.Description='HomeworkGate — контроль домашних заданий';" ^
    "$s.Save()" 2>nul

if exist "%SHORTCUT%" (
    echo     ✓ Ярлык создан на рабочем столе
) else (
    echo     ⚠ Ярлык не создан (создай вручную из папки %APP_OUT%)
)

:: ── Готово ────────────────────────────────────────────────────────────────────
echo.
echo  ╔══════════════════════════════════════════════════════╗
echo  ║  ✅ Установка завершена!                             ║
echo  ║                                                      ║
echo  ║  1. Запусти HomeworkGate с рабочего стола           ║
echo  ║  2. Перейди в ⚙️ Настройки → вставь API ключ       ║
echo  ║  3. В Настройках нажми "Установить Guardian Service" ║
echo  ║     (нужен один раз, защищает от обхода)            ║
echo  ║  4. Добавь задания и игры для блокировки            ║
echo  ╚══════════════════════════════════════════════════════╝
echo.

set /p LAUNCH="Запустить HomeworkGate прямо сейчас? (Y/N): "
if /i "%LAUNCH%"=="Y" (
    start "" "%EXE_PATH%"
)

pause
