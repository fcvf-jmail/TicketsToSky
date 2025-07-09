# TicketsToSky

**TicketsToSky** — это микросервисное приложение для поиска и уведомления о доступных авиабилетах. Оно позволяет пользователям подписываться на поиск билетов по заданным параметрам (например, направление и даты) и получать уведомления через Telegram, когда подходящие билеты найдены

## Назначение проекта

Проект создан для автоматизации поиска авиабилетов по заданным критериям и уведомления пользователей о подходящих предложениях. Основная цель — предоставить удобный и масштабируемый сервис, который:
- Позволяет пользователям подписываться на поиск билетов через API
- Обрабатывает запросы асинхронно, используя очереди сообщений
- Кэширует результаты для оптимизации производительности
- Отправляет уведомления через Telegram бот

Проект идеально подходит для тех, кто хочет настроить персонализированный мониторинг цен на авиабилеты и получать мгновенные уведомления о выгодных предложениях

## Схема работы

TicketsToSky состоит из трёх микросервисов и трёх вспомогательных сервисов, взаимодействующих через Docker Compose:

1. **ticketstosky-api**:
   - Основной API для управления подписками
   - Принимает запросы от пользователей (например, направление, даты, бюджет)
   - Создаёт обмены и очереди в RabbitMQ для передачи параметров поиска
   - Использует PostgreSQL для хранения подписок

2. **ticketstosky-parser**:
   - Получает параметры поиска из RabbitMQ
   - Обрабатывает запросы, выполняя поиск билетов
   - Кэширует результаты в Redis, чтобы отправлять только уникальные билеты
   - Отправляет уведомления в Telegram очередь через RabbitMQ

3. **ticketstosky-telegram-sender**:
   - Читает сообщения из Telegram очереди в RabbitMQ
   - Отправляет уведомления пользователям через Telegram бот

4. **postgres**:
   - Хранит данные о подписках пользователей (параметры поиска)

5. **rabbitmq**:
   - Обеспечивает асинхронное взаимодействие между сервисами через очереди сообщений (по умолчению `searchParamsExchange`, `telegramExchange`)

6. **redis**:
   - Кэширует информацию о подписках
   - Кэширует результаты поиска

**Схема взаимодействия**:
- Пользователь отправляет запрос на подписку через `ticketstosky-api`
- API сохраняет подписку в PostgreSQL и отправляет параметры поиска в очередь `searchParamsEvents` (RabbitMQ)
- `ticketstosky-parser` читает параметры из очереди, кэширует в Redis. Каждые 5 минут выполняет поиск, кэширует результаты и отправляет уведомления в очередь `telegramMessages`
- `ticketstosky-telegram-sender` получает сообщения из очереди `telegramMessages` и отправляет уведомления через Telegram-бот

Все сервисы объединены в сеть `ticketstosky-network` для безопасного взаимодействия

## Требования

Для запуска проекта вам понадобятся:
- **Docker** и **Docker Compose**: [Установить Docker](https://docs.docker.com/get-docker/), [Установить Docker Compose](https://docs.docker.com/compose/install/)
- **Telegram Bot Token**: Получите токен от [BotFather](https://t.me/BotFather) в Telegram

## Настройка и установка

1. **Клонируйте репозиторий**:
   ```bash
   git clone https://github.com/fcvf-jmail/TicketsToSky
   cd TicketsToSky
   ```

2. **Создайте файл `.env`**:
   - В корне проекта находится файл `.env.example` с примерами переменных окружения:
     ```plaintext
     # PostgreSQL settings
     POSTGRES_USER=example_user
     POSTGRES_PASSWORD=secure_password_123
     POSTGRES_DB=ticketstosky_db

     # RabbitMQ settings
     RABBITMQ_USER_NAME=user
     RABBITMQ_PASSWORD=password_456

     # Telegram bot settings
     TELEGRAM_BOT_TOKEN=1234567890:AAFakeBotTokenForExamplePurposeOnly

     # Redis settings
     REDIS_CONNECTION_STRING=redis:6379
     ```
   - Скопируйте `.env.example` в `.env`:
     ```bash
     cp .env.example .env
     ```
   - Отредактируйте `.env`, заменив фейковые значения на реальные:
     - `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`: Учетные данные и имя базы данных для PostgreSQL
     - `RABBITMQ_USER_NAME`, `RABBITMQ_PASSWORD`: Учетные данные для RabbitMQ
     - `TELEGRAM_BOT_TOKEN`: токен Telegram бота от @BotFather
     - `REDIS_CONNECTION_STRING`: Строка подключения к Redis (например, `redis:6379` для локального Redis)

3. **Проверьте `appsettings.json`** (опционально):
   - В корне проекта находится `appsettings.json` с настройками по умолчанию:
     ```json
     {
         "RabbitMQ": {
             "HostName": "rabbitmq",
             "VirtualHost": "/",
             "ExchangeName": "searchParamsExchange",
             "QueueName": "searchParamsEvents",
             "RoutingKey": "subscription.*",
             "TelegramExchangeName": "telegramExchange",
             "TelegramQueueName": "telegramMessages",
             "TelegramRoutingKey": "telegram.message"
         }
     }
     ```
   - Эти настройки (имена обменов и очередей RabbitMQ) редко требуют изменений. Вы можете оставить их как есть или отредактировать, если используете другие имена для обменов и очередей

## Запуск проекта

1. **Соберите и запустите контейнеры**:
   ```bash
   cd TicketsToSky
   docker-compose up --build -d
   ```

2. **Проверьте статус контейнеров**:
   ```bash
   docker ps
   ```
   Убедитесь, что все сервисы (`ticketstosky-api`, `ticketstosky-parser`, `ticketstosky-telegram-sender`, `postgres`, `rabbitmq`, `redis`) запущены

## Проверка работы

1. **Проверка API**:
   ```bash
   curl -f http://localhost:5148/api/health
   ```
   Ожидаемый ответ: `{"status":"healthy"}`

2. **Проверка Redis**:
   ```bash
   docker exec ticketstosky-redis-1 redis-cli ping
   ```
   Ожидаемый ответ: `PONG`

3. **Проверка RabbitMQ**:
   - Откройте `http://localhost:15672` в браузере
   - Войдите, используя логин и пароль из `.env` (например, `RABBITMQ_USER_NAME` и `RABBITMQ_PASSWORD`)
   - Проверьте наличие обменов (`searchParamsExchange`, `telegramExchange`) и очередей (`searchParamsEvents`, `telegramMessages`)

4. **Проверка PostgreSQL**:
   ```bash
   docker exec -it ticketstosky-postgres-1 psql -U ${POSTGRES_USER} -d ${POSTGRES_DB} -c "\dt"
   ```
   Убедитесь, что таблицы (например, `Subscriptions`) созданы

5. **Проверка логов**:
   ```bash
   docker-compose logs
   ```
   Проверьте логи всех сервисов на наличие ошибок

## Устранение неполадок

1. **Ошибка `Redis:ConnectionString`**:
   - Проверьте, что `REDIS_CONNECTION_STRING` задано в `.env`:
     ```bash
     cat .env
     ```
   - Проверьте логи `ticketstosky-parser`:
     ```bash
     docker logs ticketstosky-ticketstosky-parser-1
     ```

2. **Ошибка `RabbitMQ:UserName` или `RabbitMQ:Password`**:
   - Убедитесь, что `RABBITMQ_USER_NAME` и `RABBITMQ_PASSWORD` заданы в `.env`
   - Проверьте логи `ticketstosky-telegram-sender`:
     ```bash
     docker logs ticketstosky-ticketstosky-telegram-sender-1
     ```

3. **Ошибка Telegram-бота**:
   - Проверьте токен:
     ```bash
     curl https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/getMe
     ```
   - Убедитесь, что `TELEGRAM_BOT_TOKEN` в `.env` валиден

4. **Общие проблемы**:
   - Если сервисы не запускаются, увеличьте `start_period` в `healthcheck` для `ticketstosky-api` в `docker-compose.yml` (например, до `60s`)
   - Очистите кэш Docker и пересоберите:
     ```bash
     docker-compose build --no-cache
     docker-compose up -d
     ```


## API Документация

API сервиса `ticketstosky-api` предоставляет эндпоинты для управления подписками на поиск авиабилетов. Все эндпоинты находятся по базовому пути `/api/v1/subscriptions`. API принимает и возвращает данные в формате JSON. Все запросы должны содержать заголовок `Content-Type: application/json`

### Эндпоинты

#### 1. Создание подписки (`POST /api/v1/subscriptions`)

Создаёт новую подписку на поиск авиабилетов по заданным параметрам

**Параметры запроса** (в теле запроса, JSON):
- `ChatId` (обязательный, `long`): ID чата в Telegram для отправки уведомлений
- `MaxPrice` (обязательный, `int`): Максимальная цена билета (в рублях)
- `DepartureAirport` (обязательный, `string`): Код аэропорта вылета (IATA, например, `SVO`)
- `ArrivalAirport` (обязательный, `string`): Код аэропорта прилёта (IATA, например, `JFK`)
- `DepartureDate` (обязательный, `DateOnly`): Дата вылета в формате `YYYY-MM-DD`
- `MaxTransfersCount` (обязательный, `int`): Максимальное количество пересадок
- `MinBaggageAmount` (обязательный, `int`): Минимальное количество багажа (в кг)
- `MinHandbagsAmount` (обязательный, `int`): Минимальное количество ручной клади (в кг)

**Пример запроса**:
```bash
curl -X POST http://localhost:5148/api/v1/subscriptions \
-H "Content-Type: application/json" \
-d '{
    "ChatId": 123456789,
    "MaxPrice": 50000,
    "DepartureAirport": "SVO",
    "ArrivalAirport": "JFK",
    "DepartureDate": "2025-12-01",
    "MaxTransfersCount": 1,
    "MinBaggageAmount": 20,
    "MinHandbagsAmount": 5
}'
```

**Пример ответа** (успешный, HTTP 200):
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Ошибки**:
- `400 Bad Request`: Неверный формат данных или отсутствуют обязательные поля
- `500 Internal Server Error`: Ошибка сервера (например, проблемы с подключением к базе данных)

#### 2. Обновление подписки (`PUT /api/v1/subscriptions`)

Обновляет существующую подписку по её параметрам. Требуется указать Id подписки

**Параметры запроса** (в теле запроса, JSON):
- `Id` (обязательный, `Guid`): Уникальный идентификатор подписки
- `ChatId` (обязательный, `long`): ID чата в Telegram
- `MaxPrice` (обязательный, `int`): Максимальная цена билета
- `DepartureAirport` (обязательный, `string`): Код аэропорта вылета
- `ArrivalAirport` (обязательный, `string`): Код аэропорта прилёта
- `DepartureDate` (обязательный, `DateOnly`): Дата вылета
- `MaxTransfersCount` (обязательный, `int`): Максимальное количество пересадок
- `MinBaggageAmount` (обязательный, `int`): Минимальное количество багажа
- `MinHandbagsAmount` (обязательный, `int`): Минимальное количество ручной клади

**Пример запроса**:
```bash
curl -X PUT http://localhost:5148/api/v1/subscriptions \
-H "Content-Type: application/json" \
-d '{
    "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "ChatId": 123456789,
    "MaxPrice": 55000,
    "DepartureAirport": "SVO",
    "ArrivalAirport": "JFK",
    "DepartureDate": "2025-12-02",
    "MaxTransfersCount": 2,
    "MinBaggageAmount": 23,
    "MinHandbagsAmount": 7
}'
```

**Пример ответа** (успешный, HTTP 200):
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Ошибки**:
- `400 Bad Request`: Неверный формат данных, отсутствуют обязательные поля или подписка с указанным `Id` не найдена
- `500 Internal Server Error`: Ошибка сервера

#### 3. Удаление подписки (`DELETE /api/v1/subscriptions`)

Удаляет подписку по её идентификатору

**Параметры запроса** (в теле запроса, JSON):
- `Id` (обязательный, `Guid`): Уникальный идентификатор подписки

**Пример запроса**:
```bash
curl -X DELETE http://localhost:5148/api/v1/subscriptions \
-H "Content-Type: application/json" \
-d '{"Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
```

**Пример ответа** (успешный, HTTP 200):
Пустой ответ (без тела)

**Ошибки**:
- `400 Bad Request`: Неверный формат `Id` или подписка не найдена
- `500 Internal Server Error`: Ошибка сервера

#### 4. Получение всех подписок (`GET /api/v1/subscriptions`)

Возвращает список всех подписок

**Параметры запроса**: Отсутствуют

**Пример запроса**:
```bash
curl -X GET http://localhost:5148/api/v1/subscriptions
```

**Пример ответа** (успешный, HTTP 200):
```json
[
    {
        "Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "ChatId": 123456789,
        "MaxPrice": 50000,
        "DepartureAirport": "SVO",
        "ArrivalAirport": "JFK",
        "DepartureDate": "2025-12-01",
        "MaxTransfersCount": 1,
        "MinBaggageAmount": 20,
        "MinHandbagsAmount": 5
    },
    {
        "Id": "4fa85f64-5717-4562-b3fc-2c963f66afa7",
        "ChatId": 987654321,
        "MaxPrice": 60000,
        "DepartureAirport": "DME",
        "ArrivalAirport": "LAX",
        "DepartureDate": "2025-12-15",
        "MaxTransfersCount": 0,
        "MinBaggageAmount": 30,
        "MinHandbagsAmount": 10
    }
]
```

**Ошибки**:
- `500 Internal Server Error`: Ошибка сервера

#### 5. Обновление времени последней проверки (`PATCH /api/v1/subscriptions`)

Обновляет свойство `LastChecked` подписки на текущее время (UTC)

**Параметры запроса** (в теле запроса, JSON):
- `Id` (обязательный, `Guid`): Уникальный идентификатор подписки

**Пример запроса**:
```bash
curl -X PATCH http://localhost:5148/api/v1/subscriptions \
-H "Content-Type: application/json" \
-d '{"Id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"}'
```

**Пример ответа** (успешный, HTTP 200):
```json
"3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

**Ошибки**:
- `400 Bad Request`: Неверный формат `Id` или подписка не найдена
- `500 Internal Server Error`: Ошибка сервера

### Примечания

- Все даты в формате `YYYY-MM-DD` (ISO 8601)
- Коды аэропортов должны соответствовать стандарту IATA (3 буквы, например, `SVO`, `JFK`)
- Для тестирования API рекомендуется использовать инструменты, такие как `curl`, Postman или Swagger
- Убедитесь, что сервисы `postgres`, `rabbitmq` и `redis` запущены перед отправкой запросов к API
- В случае ошибок проверяйте логи сервиса `ticketstosky-api`:
  ```bash
  docker logs ticketstosky-ticketstosky-api-1
  ```