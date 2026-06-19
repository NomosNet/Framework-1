# frameworks-4-csharp

C# служба бронирования переговорки для практического занятия №4.

## Что реализовано

1. Машина состояний бронирования в четыре шага.
2. Хранение состояния процесса в памяти по `process_key`.
3. Идемпотентность по `idempotency_key` — повторная доставка не меняет состояние.
4. Компенсация (Saga) при сбое шага «ВыдатьДоступ».
5. JSON-логи с `correlation_id` для переходов, повторов и компенсаций.
6. Проверки живости и готовности (`/health/live`, `/health/ready`).
7. Метрики: успешные/ошибочные переходы, повторы, компенсации, средняя задержка по шагам.

## Запуск

```powershell
cd Framework4Service
dotnet run
```

Сервер слушает `http://127.0.0.1:8080`. Логи пишутся в stdout в формате JSON.

## Проверки

```powershell
dotnet test Framework4.slnx
```

## Пример сценария

```powershell
# Создать процесс
curl -s -X POST http://127.0.0.1:8080/api/process `
  -H "Content-Type: application/json" `
  -d '{"process_key": "room-101"}'

# Пройти все шаги
curl -s -X POST http://127.0.0.1:8080/api/process/room-101/event `
  -H "Content-Type: application/json" `
  -d '{"idempotency_key": "evt-1", "event": "ПринятьЗаявку"}'

curl -s -X POST http://127.0.0.1:8080/api/process/room-101/event `
  -H "Content-Type: application/json" `
  -d '{"idempotency_key": "evt-2", "event": "Забронировать"}'

curl -s -X POST http://127.0.0.1:8080/api/process/room-101/event `
  -H "Content-Type: application/json" `
  -d '{"idempotency_key": "evt-3", "event": "ВыдатьДоступ"}'

curl -s -X POST http://127.0.0.1:8080/api/process/room-101/event `
  -H "Content-Type: application/json" `
  -d '{"idempotency_key": "evt-4", "event": "Завершить"}'
```

Итоговое состояние: `Завершён`.

## API

| Метод | Путь | Описание |
|---|---|---|
| POST | `/api/process` | Создать процесс |
| POST | `/api/process/{key}/event` | Отправить событие |
| GET | `/api/process/{key}` | Получить состояние |
| GET | `/health/live` | Liveness |
| GET | `/health/ready` | Readiness |
| GET | `/metrics` | Метрики |

Заголовок `X-Correlation-ID` необязателен — если не передан, сервер сгенерирует и вернёт в теле ответа.
