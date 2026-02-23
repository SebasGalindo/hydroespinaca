using ChatbotService.Domain.Interfaces;
using ChatbotService.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ChatbotService.Infrastructure.Services;

/// <summary>
/// Servicio que, al arrancar la aplicación, lee los archivos <c>.md</c> de la carpeta
/// <c>docs/manuals/</c>, los trocea por secciones y genera embeddings para cada fragmento.
/// Los chunks resultantes se almacenan en <c>knowledge_chunks</c> con <c>source_type = "system_manual"</c>.
/// </summary>
public class KnowledgeBaseInitializer
{
    private readonly IKnowledgeChunkRepository _chunkRepository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ILogger<KnowledgeBaseInitializer> _logger;

    /// <summary>
    /// Ruta base donde están los manuales .md (relativa al Assembly location).
    /// </summary>
    private static readonly string ManualsPath = Path.Combine(
        AppContext.BaseDirectory, "docs", "manuals");

    /// <summary>
    /// Inicializa el servicio de carga de base de conocimientos.
    /// </summary>
    public KnowledgeBaseInitializer(
        IKnowledgeChunkRepository chunkRepository,
        IEmbeddingProvider embeddingProvider,
        ILogger<KnowledgeBaseInitializer> logger)
    {
        _chunkRepository = chunkRepository;
        _embeddingProvider = embeddingProvider;
        _logger = logger;
    }

    /// <summary>
    /// Lee todos los archivos .md del directorio de manuales, los trocea y vectoriza.
    /// Solo procesa archivos que aún no han sido indexados o que han cambiado.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    public async Task InitializeAsync(CancellationToken ct)
    {
        if (!Directory.Exists(ManualsPath))
        {
            _logger.LogWarning("Directorio de manuales no encontrado: {Path}. Omitiendo inicialización.", ManualsPath);
            return;
        }

        var mdFiles = Directory.GetFiles(ManualsPath, "*.md");
        _logger.LogInformation("Encontrados {Count} archivos .md en {Path}", mdFiles.Length, ManualsPath);

        foreach (var filePath in mdFiles)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var content = await File.ReadAllTextAsync(filePath, ct);

                if (string.IsNullOrWhiteSpace(content))
                {
                    _logger.LogDebug("Archivo {File} está vacío, omitiendo", fileName);
                    continue;
                }

                // Trocear por secciones de Markdown (## headers)
                var chunks = ChunkBySection(content, fileName);

                foreach (var (sectionId, sectionContent) in chunks)
                {
                    var sourceId = $"{fileName}_{sectionId}";

                    // Verificar si ya existe y si el contenido cambió
                    var existing = await _chunkRepository.GetBySourceAsync(sourceId, "system_manual", ct);
                    if (existing != null && existing.Content == sectionContent)
                    {
                        _logger.LogDebug("Chunk {SourceId} sin cambios, omitiendo", sourceId);
                        continue;
                    }

                    // Generar embedding
                    var embedding = await _embeddingProvider.GenerateEmbeddingAsync(
                        sectionContent, EmbeddingTaskType.RetrievalDocument, ct);

                    var chunk = new KnowledgeChunk
                    {
                        SourceType = "system_manual",
                        SourceId = sourceId,
                        Content = sectionContent,
                        Embedding = embedding,
                        Version = (existing?.Version ?? 0) + 1,
                        CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (!string.IsNullOrEmpty(existing?.Id))
                        chunk.SetId(existing.Id);

                    await _chunkRepository.UpsertAsync(chunk, ct);
                    _logger.LogInformation("Manual chunk indexado: {SourceId}", sourceId);

                    // Rate limiting
                    await Task.Delay(100, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando archivo de manual: {File}", filePath);
            }
        }

        _logger.LogInformation("Inicialización de base de conocimientos completada");
    }

    /// <summary>
    /// Trocea un documento Markdown por sus secciones (## headers).
    /// Si no hay headers, retorna el documento completo como un solo chunk.
    /// </summary>
    /// <param name="content">Contenido del archivo Markdown.</param>
    /// <param name="fileName">Nombre del archivo (sin extensión) para IDs.</param>
    /// <returns>Lista de tuplas (sectionId, sectionContent).</returns>
    private static List<(string Id, string Content)> ChunkBySection(string content, string fileName)
    {
        var chunks = new List<(string, string)>();
        var lines = content.Split('\n');
        var currentSection = new List<string>();
        var sectionIndex = 0;
        var sectionName = "intro";

        foreach (var line in lines)
        {
            if (line.StartsWith("## ") || line.StartsWith("# "))
            {
                // Guardar sección anterior si tiene contenido
                if (currentSection.Count > 0)
                {
                    var sectionContent = string.Join("\n", currentSection).Trim();
                    if (!string.IsNullOrWhiteSpace(sectionContent))
                    {
                        chunks.Add(($"s{sectionIndex}_{Sanitize(sectionName)}", sectionContent));
                    }
                }

                sectionIndex++;
                sectionName = line.TrimStart('#', ' ').Trim();
                currentSection = [line];
            }
            else
            {
                currentSection.Add(line);
            }
        }

        // Última sección
        if (currentSection.Count > 0)
        {
            var sectionContent = string.Join("\n", currentSection).Trim();
            if (!string.IsNullOrWhiteSpace(sectionContent))
            {
                chunks.Add(($"s{sectionIndex}_{Sanitize(sectionName)}", sectionContent));
            }
        }

        // Si no hubo secciones, retornar todo como un chunk
        if (chunks.Count == 0 && !string.IsNullOrWhiteSpace(content))
        {
            chunks.Add(("full", content.Trim()));
        }

        return chunks;
    }

    /// <summary>
    /// Sanitiza un nombre de sección para usarlo como ID.
    /// </summary>
    private static string Sanitize(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace(",", "")
            .Replace(".", "");
    }
}
