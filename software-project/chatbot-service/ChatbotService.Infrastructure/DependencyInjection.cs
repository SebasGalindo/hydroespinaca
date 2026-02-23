using ChatbotService.Domain.Interfaces;
using ChatbotService.Application.Features.Rag.Services;
using ChatbotService.Infrastructure.Configuration;
using ChatbotService.Infrastructure.Providers;
using ChatbotService.Infrastructure.Repositories;
using ChatbotService.Infrastructure.Services;
using HydroEspinaca.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatbotService.Infrastructure;

/// <summary>
/// Registra las dependencias de Infraestructura (Base de datos, servicios externos como Gemini, repositorios).
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Agrega los pipelines a las inyecciones DI de .NET 9.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ✅ MongoDB setup through Shared library
        services.AddMongoSettings(configuration);

        // ✅ Configuration options (bind from appsettings/env vars)
        services.Configure<GeminiSettings>(configuration.GetSection(GeminiSettings.SectionName));
        services.Configure<RagSettings>(configuration.GetSection(RagSettings.SectionName));

        // ✅ Repositories
        services.AddScoped<IChatSessionRepository, MongoChatSessionRepository>();
        services.AddScoped<IKnowledgeChunkRepository, MongoKnowledgeChunkRepository>();

        // ✅ Providers (Gemini API + MongoDB)
        services.AddScoped<ILlmProvider, GeminiLlmProvider>();
        services.AddScoped<IEmbeddingProvider, GeminiEmbeddingProvider>();
        services.AddScoped<IVectorStore, MongoVectorStore>();
        services.AddScoped<ILiveContextProvider, LiveContextProvider>();

        // ✅ HTTP Clients for microservices (Sensor, Actuator, Fuzzy)
        services.AddHttpClient<ISensorServiceClient, Clients.SensorServiceClient>();
        services.AddHttpClient<IActuatorServiceClient, Clients.ActuatorServiceClient>();
        services.AddHttpClient<IFuzzyServiceClient, Clients.FuzzyServiceClient>();

        // ✅ Application Services
        services.AddScoped<ContentSerializerService>();

        // ✅ Knowledge Base Initializer (lee .md de docs/manuals/ al startup)
        services.AddScoped<KnowledgeBaseInitializer>();

        return services;
    }
}
