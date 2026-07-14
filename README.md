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
  /{service-name}/
    service.md             # Service metadata (# Name is identity)
    endpoints.md           # Service endpoints (## Name is identity within service)
    /usecases/
      {use-case-name}.md   # Use cases and relation steps (file name = use case Name)
```

Identity is the domain `Name`: service names are unique globally; endpoint and use case names are unique within a service. Paths reuse those names (Unicode and spaces allowed; OS-invalid filename characters are rejected). Cross-references in markdown use `InitiatingEndpointName`, `ServiceName`, and `EndpointName`.

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
- `list_services` — all services; use returned `Name` as the service key everywhere else
- `get_service` — one service by `serviceName`, optionally with endpoints/use cases
- `get_use_case` — one use case by `serviceName` + `useCaseName`
- `list_endpoints` — endpoints for a service (`Name` unique within the service)
- `list_use_cases` — use cases for a service (`Name` unique within the service)
- `search_graph` — text search; matches include `EntityName` and `ServiceName`

**Write / mutate:**
- `upsert_service` — create/update by service `Name`
- `upsert_endpoint` — create/update by `serviceName` + endpoint `Name`
- `upsert_use_case` — create/update by `serviceName` + use case `Name`
- `upsert_relation` — create/update a step by `serviceName` + `useCaseName` + `stepName`
- `bulk_upsert_endpoint` / `bulk_upsert_relation` — same keys, partial success
- `remove_entity` — delete by entity `Name` (`serviceName` required for Endpoint and UseCase)

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
Upsert endpoint "GET /api/users" in service UserService
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
