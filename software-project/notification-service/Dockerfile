# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./software-project/notification-service ./notification-service

WORKDIR /app/notification-service

RUN dotnet restore NotificationService.sln --configfile ../NuGet.config
RUN dotnet publish NotificationService.Api/NotificationService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r notificationserviceapp && useradd -r -g notificationserviceapp notificationserviceapp
RUN chown -R notificationserviceapp:notificationserviceapp /app
USER notificationserviceapp

COPY --from=build /app/notification-service/out .

EXPOSE 8080
CMD ["dotnet", "NotificationService.Api.dll"]