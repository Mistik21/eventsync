
# Этап 1. Базовый образ ASP.NET Core
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app

EXPOSE 8080

# Этап 2. Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["eventsync.csproj", "./"]

RUN dotnet restore "eventsync.csproj"

# Копируем весь исходный код
COPY . .

# Публикуем приложение
RUN dotnet publish "eventsync.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Этап 3. Финальный контейнер
FROM base AS final

WORKDIR /app

# Копируем опубликованное приложение
COPY --from=build /app/publish .

# Копируем HTML, CSS и JS
COPY --from=build /src/UI/wwwroot /app/UI/wwwroot

# Настраиваем порт для Yandex Cloud
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Рабочий порт
EXPOSE 8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "eventsync.dll"]
