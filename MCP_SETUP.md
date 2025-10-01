# GraphifyCode MCP Server Setup for Claude Code

## Step 1: Build the Project

Build the GraphifyCode.MCP project:

```bash
cd src/backend/GraphifyCode.MCP
dotnet build --configuration Release
```

## Step 2: Set Environment Variable

Set the `GRAPHIFY_CODE_DATA_PATH` environment variable pointing to your graph data directory:

**Windows (PowerShell):**
```powershell
$env:GRAPHIFY_CODE_DATA_PATH = "C:\path\to\your\graph-data"
```

**Linux/macOS:**
```bash
export GRAPHIFY_CODE_DATA_PATH="/path/to/your/graph-data"
```

## Step 3: Add MCP Server to Claude Code

Open the Claude Code configuration file:

**Windows:**
```
%USERPROFILE%\.claude-code\config.json
```

**Linux/macOS:**
```
~/.claude-code/config.json
```

Add the following configuration to the `mcpServers` section:

```json
{
  "mcpServers": {
    "graphify-code": {
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

**Important:** Replace paths with absolute paths on your system.

## Step 4: Restart Claude Code

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
