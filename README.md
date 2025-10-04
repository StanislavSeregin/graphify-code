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

## MCP Server Setup

### Prerequisites

- .NET 9 SDK installed
- Claude Code or another MCP-compatible client

### Configuration

1. Open your Claude Code configuration file:
   ```bash
   ~/.claude.json
   ```

2. Add the GraphifyCode MCP server:
   ```json
   {
     "mcpServers": {
       "graphify-code": {
         "type": "stdio",
         "command": "dotnet",
         "args": [
           "run",
           "--project",
           "/absolute/path/to/graphify-code/src/backend/GraphifyCode.MCP/GraphifyCode.MCP.csproj"
         ],
         "env": {
           "GRAPHIFY_CODE_DATA_PATH": "/absolute/path/to/graph-data"
         }
       }
     }
   }
   ```

   **Important:** Replace with absolute paths on your system.

3. Restart Claude Code.

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

## Development

```
src/backend/
  GraphifyCode.Core/              # Models, settings
  GraphifyCode.Data/              # Data access, Markdown storage
  GraphifyCode.Markdown/          # Serialization framework
  GraphifyCode.Markdown.SourceGen/  # Source generator
  GraphifyCode.MCP/               # MCP server
```

**Tech stack:** C# (.NET 9), Markdown serialization with source generators

**Features:** Cascading deletion, idempotent operations, structured schemas

## License

MIT
