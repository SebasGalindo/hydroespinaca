# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./software-project/bi-service ./bi-service

WORKDIR /app/bi-service

RUN dotnet restore BiService.sln --configfile ../NuGet.config
RUN dotnet publish BiService.Api/BiService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r biapp && useradd -r -g biapp biapp
RUN chown -R biapp:biapp /app
USER biapp

COPY --from=build /app/bi-service/out .

EXPOSE 8080
CMD ["dotnet", "BiService.Api.dll"]
