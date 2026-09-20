# HomeworkGate v2.0 — Инструкция

## Самый быстрый способ (1 шаг)

1. Установи [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) если нет
2. **Дважды кликни `УСТАНОВИТЬ.bat`** — он соберёт проект, создаст ярлык на рабочем столе и предложит запустить

Всё.

---

## Первый запуск

1. Открой приложение → перейди в **⚙️ Настройки**
2. Вставь **Anthropic API ключ** (console.anthropic.com → API Keys)
3. Нажми **"Установить Guardian Service"** — это сделает блокировку необходимой.
   Guardian запускается с системными привилегиями, его нельзя убить через Диспетчер задач.
4. Включи **Автозапуск** — блокировка будет активна с самого старта Windows.

---

## Что изменилось в v2.0

- ✅ Исправлен конфликт имён `TaskStatus` (переименован в `HwStatus`)
- ✅ Добавлен `<UseWindowsForms>` в .csproj — теперь компилируется без ошибок
- ✅ Убрана утечка HICON в трей-иконке (вызывалась каждые 2 сек)
- ✅ Убраны неиспользуемые пакеты NuGet
- ✅ Блокировка по полному пути к .exe (не по имени процесса) — нет ложных срабатываний
- ✅ `finally` блок в проверке — прогресс-диалог всегда закрывается
- ✅ State файл перенесён в `ProgramData` (Guardian Service читает его как SYSTEM)
- ✅ **Guardian Windows Service** — отдельный процесс с системными привилегиями

---

## Структура проекта

```
HomeworkGate/
├── УСТАНОВИТЬ.bat              ← Запусти это
├── HomeworkGate.sln
├── HomeworkGate.Shared/        ← Общие модели
├── HomeworkGate.App/           ← UI + трей
└── HomeworkGate.Guardian/      ← Windows Service (блокировщик)
```

---

## Ручная сборка

```cmd
dotnet build HomeworkGate.sln -c Release
copy HomeworkGate.Guardian\bin\Release\net8.0-windows\win-x64\HomeworkGate.Guardian.exe ^
     HomeworkGate.App\bin\Release\net8.0-windows\
HomeworkGate.App\bin\Release\net8.0-windows\HomeworkGate.exe
```

---

## Как работает защита

| Слой | Что делает | Обойти через TaskMgr? |
|------|-----------|----------------------|
| BlockerService (UI) | Убивает процессы каждые 2 сек | Да, если убить HomeworkGate.exe |
| Guardian Service | То же, но как SYSTEM | ❌ Нельзя |

Рекомендуется установить оба. Guardian устанавливается кнопкой в Настройках.
