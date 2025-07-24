FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY .nuget/packages/ ./.nuget/packages/
COPY ./shared ./shared
COPY ./hardware-project/actuator-service ./actuator-service

WORKDIR /app/actuator-service

RUN dotnet restore ActuatorService.sln --configfile ../NuGet.config
RUN dotnet build ActuatorService.Api/ActuatorService.Api.csproj -c Debug -o out

EXPOSE 8080
CMD ["dotnet", "out/ActuatorService.Api.dll"]
