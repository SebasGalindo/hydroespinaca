# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./hardware-project/sensor-service ./sensor-service

WORKDIR /app/sensor-service

RUN dotnet restore SensorService.sln --configfile ../NuGet.config
RUN dotnet publish SensorService.Api/SensorService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r sensorapp && useradd -r -g sensorapp sensorapp
RUN chown -R sensorapp:sensorapp /app
USER sensorapp

COPY --from=build /app/sensor-service/out .

EXPOSE 8080
CMD ["dotnet", "SensorService.Api.dll"]
    