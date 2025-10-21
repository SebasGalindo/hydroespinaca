# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./software-project/bff-service ./bff-service

WORKDIR /app/bff-service

RUN dotnet restore BffService.sln --configfile ../NuGet.config
RUN dotnet publish BffService.Api/BffService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r bffserviceapp && useradd -r -g bffserviceapp bffserviceapp
RUN chown -R bffserviceapp:bffserviceapp /app
USER bffserviceapp

COPY --from=build /app/bff-service/out .

EXPOSE 8080
CMD ["dotnet", "BffService.Api.dll"]