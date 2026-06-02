# GraphifyCode

Microservices documentation through MCP tools. Markdown storage, incremental access, graph visualization.

## What & Why

Documentation for distributed systems needs structure. GraphifyCode enforces schema through MCP tools while keeping data human-readable as Markdown.

Instead of loading entire documentation, query specific pieces: services, endpoints, relations. Update programmatically. Visualize the result as an interactive graph.

## Key Features

- **Incremental queries** - `list_services`, `get_service`, `get_use_case`, `list_endpoints`, `list_use_cases`, `search_graph`
- **Schema-enforced updates** - `upsert_service`, `upsert_endpoint`, `upsert_use_case`, `upsert_relation`, `bulk_upsert_endpoint`, `bulk_upsert_relation`, `remove_entity`
- **Markdown storage** - Human-readable, version controllable, lives with your code
- **Graph visualization** - Explore documented architecture visually
- **Code navigation** - Direct links from documentation to source files

## Core Concepts

- **Services**: Applications, microservices, or external systems
- **Endpoints**: Entry points (HTTP APIs, queue consumers, background jobs)
- **Relations**: Dependencies between services (service A → endpoint in service B)

## Data Storage

```
/graph-data/
  /{service-guid}/
    service.md             # Service metadata
    endpoints.md           # Service endpoints
    /usecases/
      {usecase-guid}.md    # Use cases and relation steps
```

Markdown files with guaranteed schema, queryable through MCP tools.

## Deployment

### Docker

```bash
cp .env.example .env
# Edit .env (set DATA_PATH to your graph data directory)
docker-compose up -d --build
```

Services:
- **frontend** - http://localhost
- **backend** - Internal API (used by frontend)
- **mcp** - http://localhost:5001

Configure MCP client:
```json
{
  "mcpServers": {
    "graphify-code": {
      "type": "http",
      "url": "http://localhost:5001"
    }
  }
}
```

### Available MCP Tools

All tools return a unified response envelope:
- `ok: boolean`
- `data: object | null`
- `error: { code, message, details, retriable } | null`
- `warnings: string[]`

`error.details` is typed by error category:
- `ValidationErrorDetails`
- `NotFoundErrorDetails`
- `ConflictErrorDetails`
- `BatchErrorDetails`

**Read / discover:**
- `list_services` - All services with compact metadata and markdown snapshot
- `get_service` - One service by ID, with optional endpoints/use cases
- `get_use_case` - One use case by ID with detailed steps
- `list_endpoints` - Endpoints for a service without loading full service details
- `list_use_cases` - Use cases for a service without loading full service details
- `search_graph` - Text search across service/endpoint/use case names, descriptions, and code paths

**Write / mutate:**
- `upsert_service` - Create or update service
- `upsert_endpoint` - Create or update endpoint in service
- `upsert_use_case` - Create or update use case in service
- `upsert_relation` - Create or update use case step relation
- `bulk_upsert_endpoint` - Batch endpoint upsert with partial success reporting
- `bulk_upsert_relation` - Batch relation upsert with partial success reporting
- `remove_entity` - Remove `Service` / `Endpoint` / `UseCase` by typed enum

### Batch Behavior

`bulk_upsert_endpoint` and `bulk_upsert_relation` use **partial success** semantics:
- valid items are applied;
- failed items are returned in `data.failed[]` with per-item error details;
- `ok=true` when at least one item succeeds;
- `ok=false` with `error.code="batch_failed"` when all items fail.

### Usage Examples

```
Analyze my microservices and document the architecture
```

```
What are the dependencies of the User Service?
```

```
Create a service called "User Service" for authentication responsibilities
```

```
Find all entities related to "payments"
```

```
Upsert endpoint "GET /api/users" in service {service-id}
```

## Architecture

GraphifyCode consists of three services:

- **Frontend** - Angular app with D3.js graph visualization
- **Backend API** - ASP.NET Core REST API for serving graph data
- **MCP Server** - HTTP-based MCP tools for documentation management

## Development

### Project Structure

```
src/backend/
  GraphifyCode.Data/              # Data access, Markdown storage
  GraphifyCode.Markdown/          # Serialization framework
  GraphifyCode.Markdown.SourceGen/  # Source generator
  GraphifyCode.Api/               # Backend REST API
  GraphifyCode.MCP/               # MCP server (HTTP)

src/frontend/
  Angular 18 app                  # Graph visualization UI
```

### Tech Stack

- **Backend:** C# (.NET 9), ASP.NET Core
- **Frontend:** Angular 18, D3.js, Angular Material
- **MCP Server:** ModelContextProtocol.AspNetCore (HTTP transport)
- **Storage:** Markdown with source-generated serialization

### Features

- Cascading deletion
- Idempotent operations
- Structured schemas
- Real-time graph visualization
- Direct code navigation from docs

## License

MIT
