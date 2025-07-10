FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

COPY . .

RUN dotnet restore ./SensorService.Api/SensorService.Api.csproj
RUN dotnet build ./SensorService.Api/SensorService.Api.csproj -c Debug -o out

EXPOSE 80

CMD ["dotnet", "out/SensorService.Api.dll"]
