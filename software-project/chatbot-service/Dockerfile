# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY NuGet.config ./NuGet.config
COPY deploy-artifacts ./deploy-artifacts
COPY ./software-project/chatbot-service ./chatbot-service

WORKDIR /app/chatbot-service

RUN dotnet restore ChatbotService.sln --configfile ../NuGet.config
RUN dotnet publish ChatbotService.Api/ChatbotService.Api.csproj -c Release -o out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Create non-root user for security
RUN groupadd -r chatbotapp && useradd -r -g chatbotapp chatbotapp
RUN chown -R chatbotapp:chatbotapp /app
USER chatbotapp

COPY --from=build /app/chatbot-service/out .

# Copy knowledge base manuals
COPY --from=build /app/chatbot-service/ChatbotService.Infrastructure/docs/manuals/ ./docs/manuals/

EXPOSE 8080
CMD ["dotnet", "ChatbotService.Api.dll"]
