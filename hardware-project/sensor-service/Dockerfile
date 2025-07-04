# Dockerfile for sensor-service
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY . .
RUN dotnet restore && dotnet build
CMD ["dotnet", "SensorService.Api.dll"]
