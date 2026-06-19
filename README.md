# Технологии разработки приложений на базе фреймворков

Репозиторий с решениями четырёх практических занятий на C# / ASP.NET Core (.NET 10).

## Проекты

| № | Папка | Тема | Кратко |
|---|-------|------|--------|
| 1 | [frameworks-1-csharp](frameworks-1-csharp/) | Конвейер обработки запросов | Middleware-пайплайн, in-memory CRUD пользователей, единый формат ошибок |
| 2 | [frameworks-2-csharp](frameworks-2-csharp/) | Модульное приложение и DI | Ядро + плагины из `modules/`, топологическая сортировка зависимостей |
| 3 | [frameworks-3-csharp](frameworks-3-csharp/) | Конфигурация и защита службы | Настройки file/env/CLI, CORS, rate limit, security headers, learning/production |
| 4 | [frameworks-4-csharp](frameworks-4-csharp/) | Машина состояний и наблюдаемость | Бронирование переговорки, идемпотентность, Saga-компенсация, health/metrics |

Подробные инструкции — в README каждого проекта.

## Быстрый старт

### Задание 1 — UserService

```powershell
cd frameworks-1-csharp/UserService
dotnet run
```

Тесты (в отдельном терминале):

```powershell
cd frameworks-1-csharp/UserService.Tests
dotnet test
```

Порт: `http://127.0.0.1:8080`

### Задание 2 — модульное приложение

```powershell
cd frameworks-2-csharp
dotnet run --project Framework2/Framework2.csproj
```

Тесты:

```powershell
cd frameworks-2-csharp
dotnet test Framework2.sln
```

### Задание 3 — конфигурация и безопасность

```powershell
cd frameworks-3-csharp/Framework3Service
$env:APP_MODE = "learning"
dotnet run
```

Тесты:

```powershell
cd frameworks-3-csharp
dotnet test Framework3.slnx
```

### Задание 4 — бронирование переговорки

```powershell
cd frameworks-4-csharp/Framework4Service
dotnet run
```

Тесты:

```powershell
cd frameworks-4-csharp
dotnet test Framework4.slnx
```

Порт: `http://127.0.0.1:8080`

## Требования

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Структура репозитория

```
Framework-1/
├── README.md
├── frameworks-1-csharp/    # Задание 1: middleware, UserService
├── frameworks-2-csharp/    # Задание 2: модули, DI
├── frameworks-3-csharp/    # Задание 3: config, CORS, rate limit
└── frameworks-4-csharp/    # Задание 4: state machine, observability
```
