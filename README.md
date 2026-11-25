жж# eventsync

## Локальный запуск

Для сборки фронтенда нужны Node.js 24 и pnpm. Из корня репозитория:

```sh
pnpm --dir UI/frontend install --frozen-lockfile
pnpm --dir UI/frontend build
dotnet run
```

ASP.NET Core отдаёт собранный фронтенд на порту `8080` по умолчанию. Для работы с API приложению нужны штатные настройки PostgreSQL и Telegram. Для разработки с обновлением по мере правок запустите `pnpm --dir UI/frontend dev` рядом с ASP.NET Core. Vite отправляет запросы `/api` на порт `8080`.

Docker собирает фронтенд автоматически и включает его в образ приложения.
