using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using RAG_RBAC.Models;
using RAG_RBAC.Services;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════
//  1. Configuration — Load settings from appsettings.json
// ═══════════════════════════════════════════════════════════════════
var openAiConfig = builder.Configuration.GetSection("OpenAI");
var pineconeConfig = builder.Configuration.GetSection("Pinecone");

// ═══════════════════════════════════════════════════════════════════
//  2. Semantic Kernel — Configure LLM and embedding services
// ═══════════════════════════════════════════════════════════════════
var kernelBuilder = Kernel.CreateBuilder();

kernelBuilder.AddOpenAIChatCompletion(
    modelId: openAiConfig["ChatModel"]!,
    apiKey: openAiConfig["ApiKey"]!
);

kernelBuilder.AddOpenAITextEmbeddingGeneration(
    modelId: openAiConfig["EmbeddingModel"]!,
    apiKey: openAiConfig["ApiKey"]!
);

var kernel = kernelBuilder.Build();

builder.Services.AddSingleton(kernel);
builder.Services.AddSingleton(kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>());

// ═══════════════════════════════════════════════════════════════════
//  3. Pinecone Vector Store — Initialize client and collection
// ═══════════════════════════════════════════════════════════════════
var pineconeStore = new PineconeVectorStore(new Pinecone.PineconeClient(pineconeConfig["ApiKey"]!));

var collection = pineconeStore.GetCollection<string, RagDocument>(
    pineconeConfig["IndexName"]!
);

builder.Services.AddSingleton<IVectorStoreRecordCollection<string, RagDocument>>(collection);

// ═══════════════════════════════════════════════════════════════════
//  4. Application Services — Register DI dependencies
// ═══════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<RbacService>();
builder.Services.AddScoped<IngestionService>();
builder.Services.AddScoped<RagQueryService>();

// Swagger / OpenAPI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "RAG_RBAC API", Version = "v1" });
});

var app = builder.Build();

// ═══════════════════════════════════════════════════════════════════
//  5. Middleware Pipeline
// ═══════════════════════════════════════════════════════════════════
app.UseSwagger();
app.UseSwaggerUI();

// ═══════════════════════════════════════════════════════════════════
//  6. API Endpoints
// ═══════════════════════════════════════════════════════════════════

/// <summary>Health check endpoint to verify the service is running.</summary>
app.MapGet("/", () => Results.Ok(new
{
    Service = "RAG_RBAC",
    Status = "Running",
    Stack = "Semantic Kernel + Pinecone + OpenAI + RBAC"
}))
.WithTags("Health");

/// <summary>Returns all registered users with their roles and accessible role levels.</summary>
app.MapGet("/api/users", (RbacService rbac) =>
{
    var users = rbac.GetAllUsers().Select(u => new
    {
        u.UserId,
        u.DisplayName,
        Role = u.Role.ToString(),
        AccessibleRoles = u.GetAccessibleRoles()
    });
    return Results.Ok(users);
})
.WithTags("RBAC")
.WithName("GetAllUsers")
.WithSummary("List all hardcoded users with their roles and access levels");

/// <summary>Ingests a single document with RBAC metadata into the Pinecone vector store.</summary>
app.MapPost("/api/ingest", async (IngestRequest request, IngestionService ingestion) =>
{
    var id = await ingestion.IngestDocumentAsync(request);
    return Results.Created($"/api/documents/{id}", new { DocumentId = id });
})
.WithTags("Ingestion")
.WithName("IngestDocument")
.WithSummary("Ingest a document with role-based access metadata");

/// <summary>Seeds the vector store with 23 sample documents across all five RBAC role levels.</summary>
app.MapPost("/api/ingest/seed", async (IngestionService ingestion) =>
{
    var docs = SeedData.GetSampleDocuments();
    var ids = await ingestion.IngestBatchAsync(docs);
    return Results.Ok(new
    {
        Message = $"Seeded {ids.Count} documents across all role levels",
        DocumentIds = ids
    });
})
.WithTags("Ingestion")
.WithName("SeedDocuments")
.WithSummary("Seed the vector store with sample docs at different role levels");

/// <summary>
/// Executes an RBAC-enforced RAG query. Retrieves documents filtered by the user's role
/// and generates a grounded answer using GPT-4o. Returns 401 if the user is not found.
/// </summary>
app.MapPost("/api/query", async (QueryRequest request, RagQueryService ragService) =>
{
    try
    {
        var response = await ragService.QueryAsync(request);
        return Results.Ok(response);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Unauthorized();
    }
})
.WithTags("RAG Query")
.WithName("QueryWithRbac")
.WithSummary("Ask a question. Results are filtered by the user's role level.");

/// <summary>
/// Demo endpoint that runs the same query as every registered user,
/// showcasing how RBAC filtering produces different results per role level.
/// </summary>
app.MapGet("/api/demo/rbac-comparison", async (
    string question,
    RagQueryService ragService,
    RbacService rbac) =>
{
    var results = new Dictionary<string, object>();

    foreach (var user in rbac.GetAllUsers())
    {
        try
        {
            var response = await ragService.QueryAsync(new QueryRequest(
                UserId: user.UserId,
                Question: question,
                TopK: 3
            ));

            results[user.DisplayName] = new
            {
                Role = user.Role.ToString(),
                response.Answer,
                SourceCount = response.Sources.Count,
                Sources = response.Sources.Select(s => s.Title)
            };
        }
        catch (Exception ex)
        {
            results[user.DisplayName] = new { Error = ex.Message };
        }
    }

    return Results.Ok(results);
})
.WithTags("Demo")
.WithName("RbacComparison")
.WithSummary("Ask the same question as every user to see how RBAC filters results");

app.Run();
