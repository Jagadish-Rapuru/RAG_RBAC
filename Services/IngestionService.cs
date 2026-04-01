using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Embeddings;
using RAG_RBAC.Models;

namespace RAG_RBAC.Services;

/// <summary>
/// Handles document ingestion into the Pinecone vector store.
/// Generates text embeddings via OpenAI and stores them alongside RBAC metadata
/// to enable role-based filtering during retrieval.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a <b>scoped</b> service in the DI container.
/// </para>
/// <para>
/// Ingestion flow:
/// <list type="number">
///   <item>Generate a 1536-dimension embedding using text-embedding-ada-002.</item>
///   <item>Create a <see cref="RagDocument"/> with the content, embedding, and RBAC metadata.</item>
///   <item>Upsert the record into the Pinecone index.</item>
/// </list>
/// </para>
/// </remarks>
public class IngestionService
{
    private readonly IVectorStoreRecordCollection<string, RagDocument> _collection;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly ILogger<IngestionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionService"/> class.
    /// </summary>
    /// <param name="collection">The Pinecone vector store collection for document storage.</param>
    /// <param name="embeddingService">The OpenAI embedding generation service.</param>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    public IngestionService(
        IVectorStoreRecordCollection<string, RagDocument> collection,
        ITextEmbeddingGenerationService embeddingService,
        ILogger<IngestionService> logger)
    {
        _collection = collection;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    /// <summary>
    /// Ingests a single document by generating its embedding and upserting it to Pinecone.
    /// The <see cref="IngestRequest.MinimumAccessRole"/> metadata enables RBAC-filtered retrieval at query time.
    /// </summary>
    /// <param name="request">The document content and metadata to ingest.</param>
    /// <returns>The unique document ID assigned by the vector store.</returns>
    /// <exception cref="VectorStoreOperationException">
    /// Thrown when the Pinecone upsert operation fails (e.g., authentication error, network issue).
    /// </exception>
    public async Task<string> IngestDocumentAsync(IngestRequest request)
    {
        var embedding = await _embeddingService.GenerateEmbeddingAsync(request.Content);

        var doc = new RagDocument
        {
            Id = Guid.NewGuid().ToString(),
            Title = request.Title,
            Content = request.Content,
            Source = request.Source,
            MinimumAccessRole = request.MinimumAccessRole,
            Department = request.Department,
            Embedding = embedding
        };

        await _collection.CreateCollectionIfNotExistsAsync();
        var recordId = await _collection.UpsertAsync(doc);

        _logger.LogInformation(
            "Ingested document '{Title}' with role={Role}, department={Dept}, id={Id}",
            doc.Title, doc.MinimumAccessRole, doc.Department, recordId);

        return recordId;
    }

    /// <summary>
    /// Ingests multiple documents sequentially.
    /// Each document is embedded and upserted individually to ensure per-document error isolation.
    /// </summary>
    /// <param name="requests">The list of documents to ingest.</param>
    /// <returns>A list of document IDs for all successfully ingested documents.</returns>
    public async Task<List<string>> IngestBatchAsync(List<IngestRequest> requests)
    {
        var ids = new List<string>();
        foreach (var request in requests)
        {
            var id = await IngestDocumentAsync(request);
            ids.Add(id);
        }
        return ids;
    }
}
