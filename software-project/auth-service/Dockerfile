# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./software-project/auth-service ./auth-service

WORKDIR /app/auth-service

RUN dotnet restore AuthService.sln --configfile ../NuGet.config
RUN dotnet publish AuthService.Api/AuthService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r authapp && useradd -r -g authapp authapp
RUN chown -R authapp:authapp /app
USER authapp

COPY --from=build /app/auth-service/out .

EXPOSE 8080
CMD ["dotnet", "AuthService.Api.dll"]
