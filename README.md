# GraphifyCode

Microservices documentation through MCP tools. Markdown storage, incremental access, graph visualization.

## What & Why

Documentation for distributed systems needs structure. GraphifyCode enforces schema through MCP tools while keeping data human-readable as Markdown.

Instead of loading entire documentation, query specific pieces: services, endpoints, relations. Update programmatically. Visualize the result as an interactive graph.

## Key Features

- **Incremental queries** - GetServices, GetEndpoints, GetRelations without loading everything
- **Schema-enforced updates** - CreateOrUpdateService/Endpoint, AddRelation with guaranteed structure
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
    relations.md           # Service relations
```

Markdown files with guaranteed schema, queryable through MCP tools.

## Deployment

### Docker (Recommended)

```bash
cp .env.example .env
# Edit .env with your data path (DATA_PATH)
docker-compose up -d
```

Access:
- Frontend: http://localhost
- MCP Server: http://localhost:5001

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

### Local Development

**Prerequisites:** .NET 9 SDK, Node.js 20+

**MCP Server (stdio mode):**

Edit `~/.claude.json`:
```json
{
  "mcpServers": {
    "graphify-code": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/GraphifyCode.MCP/GraphifyCode.MCP.csproj"],
      "env": {
        "DATA_PATH": "/path/to/graph-data"
      }
    }
  }
}
```

### Available Tools

**Query:**
- `GetServices` - All services with metadata
- `GetEndpoints` - Endpoints for a service
- `GetRelations` - Dependencies for a service

**Modify:**
- `CreateOrUpdateService` / `CreateOrUpdateEndpoint` - Add or update
- `AddRelation` - Create dependency
- `DeleteService` / `DeleteEndpoint` / `DeleteRelation` - Remove (cascading)

### Usage Examples

```
Analyze my microservices and document the architecture
```

```
What are the dependencies of the User Service?
```

```
Add a new endpoint "GET /api/users" to the User Service
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
