# GraphifyCode

Visualize your codebase as an interactive graph. Map microservices, their entry points, and dependencies to understand and navigate your architecture.

## Overview

GraphifyCode helps you:
- **Visualize microservices** and their purpose
- **Discover entry points** (HTTP APIs, message queues, background jobs) with descriptions
- **Map connections** between services and their dependencies
- **Navigate to code** quickly with direct links to source files

## Technology Stack

- **Backend**: C# (.NET 9)
- **Data Format**: Markdown (human-readable, version control friendly)
- **MCP Integration**: Model Context Protocol for AI-assisted graph construction
- **Frontend**: Angular + TypeScript (planned)

## Architecture

### Core Concepts

- **Services**: Distinct applications, microservices, or external systems
- **Endpoints**: Entry points to services (HTTP APIs, queue consumers, background jobs)
- **Relations**: Dependencies between services (service A calls endpoint in service B)

### Data Storage

Data is stored in Markdown format for human readability and version control:

```
/graph-data/
  /{service-guid}/         # Directory named by service GUID
    service.md             # Service metadata
    endpoints.md           # Service endpoints
    relations.md           # Service relations (target endpoint IDs)
```

Services are identified by GUIDs to ensure uniqueness and avoid naming conflicts.

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

### Available MCP Tools

#### Discovery
- `GetServices` - Get all services with metadata
- `GetEndpoints` - Get endpoints for a service
- `GetRelations` - Get dependencies for a service

#### Modification
- `CreateOrUpdateService` - Create or update a service
- `CreateOrUpdateEndpoint` - Create or update an endpoint
- `AddRelation` - Add service dependency
- `DeleteService` - Delete service (cascading)
- `DeleteEndpoint` - Delete endpoint (cascading)
- `DeleteRelation` - Delete specific dependency

### Usage Examples

Ask Claude Code to:

```
Analyze my microservices and create a dependency graph
```

```
Show me all services and their endpoints
```

```
What services depend on the User Service?
```

```
Create a new service called "Payment Service" at src/services/payment
```

## Development

### Project Structure

```
src/backend/
  GraphifyCode.Core/              # Core models and settings
  GraphifyCode.Data/              # Data access with Markdown storage
  GraphifyCode.Markdown/          # Markdown serialization framework
  GraphifyCode.Markdown.SourceGen/  # Source generator for Markdown serialization
  GraphifyCode.MCP/               # MCP server implementation
```

### Key Features

- **Markdown Serialization**: Custom source generator for human-readable data
- **Cascading Deletion**: Automatic cleanup of orphaned relations
- **Idempotent Operations**: Safe to retry operations
- **AI-First Design**: Rich descriptions for AI understanding

## License

MIT
