FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY .nuget/packages/ ./.nuget/packages/
COPY ./shared ./shared
COPY ./hardware-project/sensor-service ./sensor-service

WORKDIR /app/sensor-service

RUN dotnet restore SensorService.sln --configfile ../NuGet.config
RUN dotnet build SensorService.Api/SensorService.Api.csproj -c Debug -o out

EXPOSE 8080
CMD ["dotnet", "out/SensorService.Api.dll"]
    