# Окружение — Desktop QA Sandbox

## Стенд

| Компонент | Значение |
|-----------|----------|
| ОС | Windows 11, 10.0.26300 |
| Процессор | x64 |
| .NET SDK | 11 (TFM `net11.0-windows`) |
| UI-фреймворк | Windows Forms |
| Шрифт | Segoe UI 9.5–15 pt |
| Экран | 1920×1080, масштаб 100 % |
| Права | обычный пользователь, без администратора |

## Приложение

| Параметр | Значение |
|----------|----------|
| Файл | `src/DesktopSandbox/bin/Release/net11.0-windows/DesktopSandbox.exe` |
| Размер окна | 1040×760, минимум 860×620 |
| Разделы | Registration, Orders, Notes |
| Файл заметки | `%TEMP%\qa-sandbox-note.txt` |
| Хранение данных | в памяти; на диск пишется только заметка |

## Инструменты

| Инструмент | Версия/роль |
|------------|-------------|
| .NET SDK | 11 |
| `dotnet build` | сборка Release |
| System.Windows.Automation | UI Automation (UIA) |
| PowerShell 5.1 | запуск, скриншоты, отладка |
| `PrintWindow` (user32) | захват окна без фокуса |
| Markdown | отчётность |

## Как собрать

```powershell
dotnet build src/DesktopSandbox/DesktopSandbox.csproj -c Release
dotnet build automation/DesktopSandbox.UiChecks/DesktopSandbox.UiChecks.csproj -c Release
```

Ожидаемый результат: 0 предупреждений, 0 ошибок.

## Как запустить

```powershell
src\DesktopSandbox\bin\Release\net11.0-windows\DesktopSandbox.exe
```

## Как прогнать проверки

```powershell
# UI-прогон: приложение запускается и управляется через UIA
automation\DesktopSandbox.UiChecks\bin\Release\net11.0-windows\DesktopSandbox.UiChecks.exe `
  src\DesktopSandbox\bin\Release\net11.0-windows\DesktopSandbox.exe

# Логические проверки без UI (подходит для CI)
dotnet run --project automation/DesktopSandbox.UiChecks -c Release -- --logic
```

## Как обновить скриншоты

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File automation/Capture-Evidence.ps1
```

Скрипт сам запускает приложение, выполняет сценарии и сохраняет PNG в `evidence/`.

## Особенности окружения

- UI Automation требует **интерактивного рабочего стола**; в headless-агенте UI-прогон не запустится,
  поэтому для CI предусмотрен режим `--logic`.
- `PrintWindow` работает даже когда окно не в фокусе — это важно при параллельной работе.
- Формат чисел фиксирован `InvariantCulture`, чтобы локаль (запятая/точка) не ломала проверки.
- После UI-прогона процесс приложения принудительно завершается; перед повторным запуском убедитесь,
  что старых процессов нет (`Stop-Process -Name DesktopSandbox`).
