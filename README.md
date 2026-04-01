# RAG_RBAC

**Semantic Kernel + RAG + Role-Based Access Control**

A .NET 8 Minimal API that implements a Retrieval-Augmented Generation pipeline with document-level access control and role-based query filtering using hardcoded roles.

## Tech Stack

| Layer             | Technology                        |
|-------------------|-----------------------------------|
| Orchestration     | Microsoft Semantic Kernel 1.30    |
| LLM               | Azure OpenAI GPT-4o              |
| Embeddings        | Azure OpenAI text-embedding-ada-002 (1536d) |
| Vector Store      | Pinecone (metadata filtering)    |
| RBAC              | Hardcoded roles with hierarchical access |
| API               | ASP.NET Minimal APIs + Swagger   |

## Architecture and RBAC Flow

```
User Query ("What is the Q4 budget?")
        |
        v
  ┌─────────────────┐
  │  RbacService     │  --> Identify user, resolve accessible roles
  │  (Hardcoded)     │      Developer = [Intern, Developer]
  └────────┬────────┘
           v
  ┌─────────────────┐
  │  Embedding       │  --> Convert question to 1536-dim vector
  │  (ada-002)       │
  └────────┬────────┘
           v
  ┌─────────────────┐
  │  Pinecone Search │  --> Vector search WITH metadata filter:
  │  + RBAC Filter   │      MinimumAccessRole IN [Intern, Developer]
  └────────┬────────┘      (Manager/Director/Admin docs excluded)
           v
  ┌─────────────────┐
  │  GPT-4o          │  --> Generate grounded answer from filtered context
  │  (Semantic Kernel)│
  └─────────────────┘
```

**Key insight:** RBAC is enforced at the vector search layer via Pinecone metadata filtering. Documents the user cannot access are never retrieved, so they never enter the LLM context window. This is more secure than post-retrieval filtering because sensitive content is never exposed to the model.

## Role Hierarchy

| Role     | Level | Can Access                              |
|----------|-------|-----------------------------------------|
| Intern   | 0     | Intern docs only                        |
| Developer| 1     | Intern + Developer docs                 |
| Manager  | 2     | Intern + Developer + Manager docs       |
| Director | 3     | All except Admin docs                   |
| Admin    | 4     | Everything                              |

## Hardcoded Test Users

| UserId    | Name               | Role     |
|-----------|--------------------|----------|
| intern01  | Alex (Intern)      | Intern   |
| dev01     | Jagadish (Developer)| Developer|
| mgr01     | Rohit (Manager)    | Manager  |
| dir01     | Sarah (Director)   | Director |
| admin01   | System Admin       | Admin    |

## Quick Start

### 1. Prerequisites

- .NET 8 SDK
- Azure OpenAI resource with `gpt-4o` and `text-embedding-ada-002` deployments
- Pinecone account with an index named `rbac-rag-docs` (1536 dimensions, cosine metric)

### 2. Configure

Update `appsettings.json` with your credentials:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
    "ApiKey": "YOUR-KEY",
    "ChatDeployment": "gpt-4o",
    "EmbeddingDeployment": "text-embedding-ada-002"
  },
  "Pinecone": {
    "ApiKey": "YOUR-PINECONE-KEY",
    "IndexName": "rbac-rag-docs"
  }
}
```

### 3. Run

```bash
dotnet restore
dotnet run
```

### 4. Seed and Test

```bash
# Seed sample documents (8 docs across all role levels)
curl -X POST http://localhost:5000/api/ingest/seed

# Query as a Developer (sees Intern + Developer docs only)
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"userId": "dev01", "question": "What is our cloud infrastructure?", "topK": 3}'

# Query as an Intern (same question, fewer results)
curl -X POST http://localhost:5000/api/query \
  -H "Content-Type: application/json" \
  -d '{"userId": "intern01", "question": "What is our cloud infrastructure?", "topK": 3}'

# RBAC Comparison Demo: same question, all users
curl "http://localhost:5000/api/demo/rbac-comparison?question=What%20is%20the%20Q4%20budget"
```

## API Endpoints

| Method | Endpoint                    | Description                            |
|--------|-----------------------------|----------------------------------------|
| GET    | /api/users                  | List all users and their access levels |
| POST   | /api/ingest                 | Ingest a single document with role tag |
| POST   | /api/ingest/seed            | Seed 8 sample documents                |
| POST   | /api/query                  | Query with RBAC-filtered RAG           |
| GET    | /api/demo/rbac-comparison   | Same question across all user roles    |

## Project Structure

```
RAG_RBAC/
├── Models/
│   ├── AppUser.cs          # User model + Role enum (5 levels)
│   ├── RagDocument.cs       # Pinecone vector record with role metadata
│   └── Dtos.cs             # Request/Response DTOs
├── Services/
│   ├── RbacService.cs       # Hardcoded users + role access logic
│   ├── IngestionService.cs  # Embed + store docs in Pinecone
│   ├── RagQueryService.cs   # RBAC-filtered RAG pipeline
│   └── SeedData.cs         # Sample docs at every role level
├── Program.cs              # DI + Semantic Kernel config + Minimal API
├── appsettings.json        # Azure OpenAI + Pinecone config
└── RAG_RBAC.csproj      # NuGet packages
```

## Next Steps (Production Upgrades)

- Replace hardcoded roles with Azure AD / ASP.NET Identity
- Add JWT authentication middleware
- Implement document chunking for large files
- Add Pinecone namespace-per-tenant for multi-tenancy
- Add Redis caching for frequent queries
- Implement audit logging for compliance
