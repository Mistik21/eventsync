
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["eventsync.csproj", "./"]
RUN dotnet restore "eventsync.csproj"
COPY . .
RUN dotnet publish "eventsync.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=build /src/UI/wwwroot /app/UI/wwwroot
ENV PORT=8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "eventsync.dll"]
