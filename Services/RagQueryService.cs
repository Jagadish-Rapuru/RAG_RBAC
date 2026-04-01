using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using RAG_RBAC.Models;

namespace RAG_RBAC.Services;

/// <summary>
/// Implements the core Retrieval-Augmented Generation (RAG) pipeline with RBAC enforcement.
/// Ensures users only receive answers grounded in documents they are authorized to access.
/// </summary>
/// <remarks>
/// <para>
/// Registered as a <b>scoped</b> service in the DI container.
/// </para>
/// <para>
/// Pipeline flow:
/// <list type="number">
///   <item>Validate the user and compute their accessible role levels.</item>
///   <item>Generate a vector embedding for the user's natural language question.</item>
///   <item>Perform a vector similarity search against Pinecone.</item>
///   <item>Fetch full records by ID and apply RBAC filtering (in-memory post-filter).</item>
///   <item>Build a grounded prompt with retrieved context and invoke GPT-4o for the final answer.</item>
/// </list>
/// </para>
/// <para>
/// <b>Security note:</b> RBAC filtering ensures that documents above the user's clearance level
/// are excluded from the LLM context, preventing information leakage through generated answers.
/// </para>
/// </remarks>
public class RagQueryService
{
    private readonly IVectorStoreRecordCollection<string, RagDocument> _collection;
    private readonly ITextEmbeddingGenerationService _embeddingService;
    private readonly Kernel _kernel;
    private readonly RbacService _rbacService;
    private readonly ILogger<RagQueryService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RagQueryService"/> class.
    /// </summary>
    /// <param name="collection">The Pinecone vector store collection for document retrieval.</param>
    /// <param name="embeddingService">The OpenAI embedding generation service for query vectorization.</param>
    /// <param name="kernel">The Semantic Kernel instance configured with GPT-4o for answer generation.</param>
    /// <param name="rbacService">The RBAC service for user authentication and role resolution.</param>
    /// <param name="logger">The logger instance for diagnostic and audit output.</param>
    public RagQueryService(
        IVectorStoreRecordCollection<string, RagDocument> collection,
        ITextEmbeddingGenerationService embeddingService,
        Kernel kernel,
        RbacService rbacService,
        ILogger<RagQueryService> logger)
    {
        _collection = collection;
        _embeddingService = embeddingService;
        _kernel = kernel;
        _rbacService = rbacService;
        _logger = logger;
    }

    /// <summary>
    /// Executes an RBAC-enforced RAG query: retrieves relevant documents filtered by the user's role,
    /// then generates a grounded answer using GPT-4o.
    /// </summary>
    /// <param name="request">The query request containing the user ID, question, and result limit.</param>
    /// <returns>
    /// A <see cref="QueryResponse"/> containing the generated answer, source documents used,
    /// the user's role, and the count of documents searched.
    /// </returns>
    /// <exception cref="UnauthorizedAccessException">
    /// Thrown when the specified <see cref="QueryRequest.UserId"/> does not match any registered user.
    /// </exception>
    public async Task<QueryResponse> QueryAsync(QueryRequest request)
    {
        // Step 1: Validate user identity and resolve their accessible role hierarchy.
        var user = _rbacService.GetUser(request.UserId)
            ?? throw new UnauthorizedAccessException($"User '{request.UserId}' not found.");

        var accessibleRoles = user.GetAccessibleRoles();

        _logger.LogInformation(
            "User '{User}' ({Role}) querying. Accessible roles: [{Roles}]",
            user.DisplayName, user.Role, string.Join(", ", accessibleRoles));

        // Step 2: Convert the natural language question into a vector embedding.
        var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Question);

        // Step 3: Perform cosine similarity search against the Pinecone index.
        // Over-fetch by 5x to account for documents that will be filtered out by RBAC.
        // The SK Pinecone connector (1.30.0-alpha) may not populate metadata fields in
        // search results, so full records are fetched separately by ID in the next step.
        var searchOptions = new VectorSearchOptions
        {
            Top = request.TopK * 5
        };

        var searchResults = await _collection.VectorizedSearchAsync(questionEmbedding, searchOptions);

        var searchHits = new List<(string Id, double Score)>();
        await foreach (var result in searchResults.Results)
        {
            searchHits.Add((result.Record.Id, result.Score ?? 0));
        }

        _logger.LogInformation("Vector search returned {Count} results", searchHits.Count);

        // Step 4: Fetch full records with metadata and apply RBAC filtering.
        // This two-phase approach (search then fetch) ensures metadata fields like
        // MinimumAccessRole are reliably populated regardless of connector behavior.
        var chunks = new List<RetrievedChunk>();
        var contextBuilder = new System.Text.StringBuilder();
        var accessibleRolesSet = new HashSet<string>(accessibleRoles, StringComparer.OrdinalIgnoreCase);

        var scoreMap = searchHits.ToDictionary(h => h.Id, h => h.Score);
        var fullRecords = new Dictionary<string, RagDocument>();
        await foreach (var record in _collection.GetBatchAsync(searchHits.Select(h => h.Id)))
        {
            if (record is not null)
                fullRecords[record.Id] = record;
        }

        foreach (var hit in searchHits.OrderByDescending(h => h.Score))
        {
            if (!fullRecords.TryGetValue(hit.Id, out var doc))
                continue;

            // RBAC enforcement: exclude documents above the user's clearance level.
            if (!accessibleRolesSet.Contains(doc.MinimumAccessRole))
            {
                _logger.LogInformation(
                    "RBAC filtered out '{Title}' (requires {Role}) for user {User} ({UserRole})",
                    doc.Title, doc.MinimumAccessRole, user.DisplayName, user.Role);
                continue;
            }

            chunks.Add(new RetrievedChunk(
                doc.Title,
                doc.Source,
                doc.Department,
                hit.Score
            ));

            contextBuilder.AppendLine($"[Source: {doc.Title} | Dept: {doc.Department}]");
            contextBuilder.AppendLine(doc.Content);
            contextBuilder.AppendLine();

            if (chunks.Count >= request.TopK)
                break;
        }

        // Step 5: Build a grounded prompt with retrieved context and invoke GPT-4o.
        // The prompt instructs the LLM to only use provided context, preventing hallucination.
        var groundedPrompt = $"""
            You are a helpful assistant that answers questions based on the provided context.
            Only use information from the context below. If the context does not contain
            enough information to answer, say so clearly.

            The user's role is: {user.Role}
            The user's name is: {user.DisplayName}

            --- CONTEXT (retrieved documents the user has access to) ---
            {contextBuilder}
            --- END CONTEXT ---

            Question: {request.Question}
            """;

        var answer = await _kernel.InvokePromptAsync<string>(groundedPrompt);

        return new QueryResponse(
            Answer: answer ?? "I could not generate an answer.",
            Sources: chunks,
            UserRole: user.Role.ToString(),
            DocumentsSearched: chunks.Count
        );
    }
}
