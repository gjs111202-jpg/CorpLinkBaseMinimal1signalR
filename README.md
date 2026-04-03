# CorpLink Base Minimal + PostgreSQL

Готовый учебный проект корпоративного мессенджера на Blazor Server (.NET 10) с PostgreSQL и Entity Framework Core.

## Что уже есть

- создание пользователей;
- выбор текущего пользователя;
- создание диалога между двумя пользователями;
- отправка сообщений;
- хранение данных в PostgreSQL.

## Что нужно установить

- .NET 10 SDK
- Visual Studio 2022/2026 с workload **ASP.NET and web development**
- PostgreSQL

## Перед первым запуском

1. Создай базу данных `corplinkdb` в PostgreSQL.
2. Открой `appsettings.json` и укажи свой пароль от PostgreSQL.
3. В терминале в папке проекта выполни:

```bash
dotnet restore
dotnet ef database update
```

Если миграции не применяются, сначала создай их так:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Запуск

```bash
dotnet run
```

или просто открой проект в Visual Studio и нажми `F5`.

## Строка подключения

По умолчанию используется:

```json
"DefaultConnection": "Host=localhost;Port=5432;Database=corplinkdb;Username=postgres;Password=postgres"
```

Поменяй `Password=postgres` на свой пароль.
