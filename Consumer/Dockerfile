# 1. Базовая среда для запуска
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# 2. Среда для компиляции
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости (кэшируемый слой)
COPY ["Consumer.csproj", "."]
RUN dotnet restore "Consumer.csproj"

# Копируем весь код и собираем
COPY . .
RUN dotnet publish "Consumer.csproj" -c Release -o /app/publish

# 3. Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Запускаем именно Consumer.dll
ENTRYPOINT ["dotnet", "Consumer.dll"]
