# syntax=docker/dockerfile:1

# --- Etapa de compilación ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restauración con caché de capas: primero los .csproj y global.json.
COPY global.json ./
COPY src/MediTrack.ReminderService.Domain/*.csproj src/MediTrack.ReminderService.Domain/
COPY src/MediTrack.ReminderService.Application/*.csproj src/MediTrack.ReminderService.Application/
COPY src/MediTrack.ReminderService.Infrastructure/*.csproj src/MediTrack.ReminderService.Infrastructure/
COPY src/MediTrack.ReminderService.Api/*.csproj src/MediTrack.ReminderService.Api/
RUN dotnet restore src/MediTrack.ReminderService.Api/MediTrack.ReminderService.Api.csproj

# Copia del resto del código y publicación.
COPY src/ src/
RUN dotnet publish src/MediTrack.ReminderService.Api/MediTrack.ReminderService.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# --- Etapa de ejecución ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MediTrack.ReminderService.Api.dll"]
