FROM mcr.microsoft.com/dotnet/sdk:9.0

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY .nuget/packages/ ./.nuget/packages/
COPY ./shared ./shared
COPY ./software-project/auth-service ./auth-service

WORKDIR /app/auth-service

RUN dotnet restore AuthService.sln --configfile ../NuGet.config
RUN dotnet build AuthService.Api/AuthService.Api.csproj -c Debug -o out

EXPOSE 8080
CMD ["dotnet", "out/AuthService.Api.dll"]
