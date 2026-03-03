# LUNA — Local Universal Neural Assistant

LUNA is a managed MCP (Model Context Protocol) platform that allows administrators to host MCP servers as Docker containers and enables users to obtain API keys to connect their AI clients to those servers.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                        Portal.LUNA                           │
│  Blazor WASM + ASP.NET Core API + SQLite                     │
│  - Admin: manage MCP server catalog & Docker containers      │
│  - Users: browse servers, generate API keys, set KVPs        │
└────────────────────────┬─────────────────────────────────────┘
                         │ Docker Socket
         ┌───────────────┼───────────────┐
         ▼               ▼               ▼
  ┌─────────────┐  ┌───────────────┐   ...
  │Ping.MCP.LUNA│  │Dev.MCP.LUNA   │
  │ (container) │  │ (container)   │
  └─────────────┘  └───────────────┘
```

---

## Projects

| Project | Description |
|---------|-------------|
| `Portal.LUNA.API` | ASP.NET Core 10 Web API backend |
| `Portal.LUNA.App` | Blazor WebAssembly frontend |
| `Portal.LUNA.Database` | EF Core + SQLite data layer |
| `Portal.LUNA.Service` | Business logic services |
| `Portal.LUNA.Dto` | Shared Data Transfer Objects |
| `Dev.MCP.LUNA` | MCP server for development workflows (sandboxes, GitHub, build tools) |
| `Ping.MCP.LUNA` | MCP server for network ping operations |

---

## Getting Started

### Prerequisites
- .NET 10 SDK
- Docker (running on the same host as Portal.LUNA)

### Running Portal.LUNA

```bash
cd LUNA
dotnet run --project Portal.LUNA.API
```

The first registered user is automatically made an Administrator.

### Docker Images (MCP Servers)

MCP server images are published to GitHub Container Registry:

```bash
# Ping MCP Server
docker pull ghcr.io/dahln/ping-mcp-luna:latest

# Dev MCP Server
docker pull ghcr.io/dahln/dev-mcp-luna:latest
```

### API Key Authentication

All MCP servers require an API key passed as a header:

```
X-API-Key: luna_<your-api-key>
```

Users generate API keys through the Portal. Each key is associated with a specific MCP server instance.

### MCP Client Configuration

After generating an API key in the Portal, configure your MCP client:

```json
{
  "mcpServers": {
    "ping": {
      "url": "http://your-host:9000/mcp",
      "headers": {
        "X-API-Key": "luna_<your-api-key>"
      }
    }
  }
}
```

---

## Dev.MCP.LUNA Key-Value Pairs

When registering an API key for `Dev.MCP.LUNA`, add the following key-value pairs in the Portal:

| Key | Value |
|-----|-------|
| `GitHubUser` | Your GitHub username |
| `GitHubPat` | Your GitHub Personal Access Token |

---

## Template: Creating a New MCP Server for LUNA

Use this template to create a new MCP server that integrates with the LUNA platform:

```
I need to create a new LUNA MCP server called [ServerName].MCP.LUNA.

Requirements:
1. Create a new ASP.NET Core 10 minimal API project named [ServerName].MCP.LUNA
2. Add the ModelContextProtocol.AspNetCore NuGet package
3. Create a Tools/ directory with a [ServerName]Tools.cs class decorated with [McpServerToolType]
4. Each tool method should be decorated with [McpServerTool] and [Description("...")]
5. The server must accept an API key via the X-API-Key HTTP header or api_key query parameter
6. API key validation middleware should reject requests with 401 Unauthorized if the key doesn't match
7. Expose a /health endpoint returning {"status": "healthy", "service": "[ServerName].MCP.LUNA"}
8. Mount the MCP endpoint at /mcp
9. Create a Dockerfile that:
   - Uses mcr.microsoft.com/dotnet/sdk:10.0 for build
   - Uses mcr.microsoft.com/dotnet/aspnet:10.0 for runtime
   - Supports both linux/amd64 and linux/arm64 platforms
   - Exposes port 8080
   - Sets ASPNETCORE_URLS=http://+:8080
10. Add appsettings.json with ApiKey and any other required config keys
11. The server should be designed so that the Portal.LUNA admin can add it to the MCP catalog
    with the Docker image name and it can be started as a container

The server should implement the following MCP tools:
[Describe your tools here]

Key-value pairs that users will configure in Portal.LUNA for this server:
[List required KVPs here]
```

---

## Sandbox Image

The `Dev.MCP.LUNA` server uses the following base sandbox image for development containers:

```bash
docker pull ghcr.io/dahln/lunasandbox:latest
```

This image includes: .NET, Python, Node.js/npm, Git, and GitHub CLI (`gh`).

---

## License

MIT
