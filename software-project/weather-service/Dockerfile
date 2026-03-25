# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./software-project/weather-service ./weather-service

WORKDIR /app/weather-service

RUN dotnet restore WeatherService.sln --configfile ../NuGet.config
RUN dotnet publish WeatherService.Api/WeatherService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r weatherapp && useradd -r -g weatherapp weatherapp
RUN chown -R weatherapp:weatherapp /app
USER weatherapp

COPY --from=build /app/weather-service/out .

EXPOSE 8080
CMD ["dotnet", "WeatherService.Api.dll"]
