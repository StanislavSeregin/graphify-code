# GraphifyCode MCP Server Setup for Claude Code

## Prerequisites

- .NET 9 SDK installed on your system

## Step 1: Add MCP Server to Claude Code

Open the Claude Code configuration file:

```
~/.claude.json
```

Add the following configuration to the `mcpServers` section:

```json
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
```

**Important:** Replace paths with absolute paths on your system.

## Step 2: Restart Claude Code

Restart Claude Code for the changes to take effect.

## Verification

After restarting, Claude Code should automatically connect to the MCP server. You can verify the following tools are available:

### Read Operations:
- `GetServicesOverview` - get overview of all services
- `GetServiceEndpoints` - get endpoints for a specific service
- `GetServiceRelations` - get relations for a specific service

### Write Operations:
- `CreateOrUpdateService` - create or update a service
- `CreateOrUpdateServiceEndpoint` - create or update an endpoint
- `AddServiceRelation` - add relation between services
- `DeleteService` - delete service (with cascading deletion)
- `DeleteServiceEndpoint` - delete endpoint (with cascading deletion)
- `DeleteServiceRelation` - delete relation

## Data Structure

Data is stored in the following structure:

```
graph-data/
  /{service-guid}/
    index.json          # ServiceNode
    endpoints.json      # ServiceEndpoints[]
    relations.json      # ServiceRelations[]
```

## Usage Example

Ask Claude Code:

```
Show me all services in the graph
```

or

```
Create a new service named "UserService" with description "Handles user operations"
```
