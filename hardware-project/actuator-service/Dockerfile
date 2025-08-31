# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY .nuget/packages/ ./.nuget/packages/
COPY deploy-artifacts ./deploy-artifacts
COPY ./hardware-project/actuator-service ./actuator-service

WORKDIR /app/actuator-service

RUN dotnet restore ActuatorService.sln --configfile ../NuGet.config
RUN dotnet publish ActuatorService.Api/ActuatorService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r actuatorapp && useradd -r -g actuatorapp actuatorapp
RUN chown -R actuatorapp:actuatorapp /app
USER actuatorapp

COPY --from=build /app/actuator-service/out .

EXPOSE 8080
CMD ["dotnet", "ActuatorService.Api.dll"]
