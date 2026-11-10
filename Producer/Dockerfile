# 1. Базовая среда для запуска
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

# 2. Среда для компиляции
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Копируем csproj и восстанавливаем зависимости (кэшируемый слой)
COPY ["Producer.csproj", "."]
RUN dotnet restore "Producer.csproj"

# Копируем весь код и собираем
COPY . .
RUN dotnet publish "Producer.csproj" -c Release -o /app/publish

# 3. Финальный образ
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Запускаем именно Producer.dll
ENTRYPOINT ["dotnet", "Producer.dll"]
