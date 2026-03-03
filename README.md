# LUNA: Local Universal Neural Assistant

LUNA is a system for managing MCP (Model Context Protocol) servers and their availability to users. Admins create and manage MCP servers; users generate API keys and connect their clients.

## Projects

| Project | Type | Description |
|---------|------|-------------|
| `Portal.LUNA.API` | .NET 10 Web API | Main portal — auth, admin, user management, sandbox orchestration |
| `Portal.LUNA.App` | Blazor WASM | Frontend SPA hosted by the API |
| `Portal.LUNA.Database` | Class Library | EF Core entities and DbContext (SQLite) |
| `Portal.LUNA.Service` | Class Library | Business logic and Docker orchestration |
| `Portal.LUNA.Dto` | Class Library | Shared data transfer objects |
| `Dev.MCP.LUNA` | .NET 10 Web API | MCP server for development tools (GitHub, sandbox, build/run) |
| `Ping.MCP.LUNA` | .NET 10 Web API | MCP server for network connectivity testing (ping) |

## Quick Start

```bash
cd LUNA
docker compose up --build
```

Access the portal at `http://localhost:5000`. The first registered user becomes admin.

## MCP Server Addresses

All MCP servers are accessible at:

```
http(s)://{ROOT_DOMAIN}/{PORT}/mcp/{MCP_SERVER_NAME}
```

- **Ping MCP**: `/mcp/ping`
- **Dev MCP**: `/mcp/dev`

The `ROOT_DOMAIN` is configurable in the Portal admin settings page.

## Architecture

```
Client (AI Agent)
    │  Authorization: Bearer {apiKey}
    ▼
Portal.LUNA.API  ─── SQLite DB
    │              (manages containers via Docker socket)
    ├── /mcp/ping ──► Ping.MCP.LUNA container
    └── /mcp/dev  ──► Dev.MCP.LUNA container
                           │
                      Sandbox containers
                    (ghcr.io/dahln/lunasandbox)
```

## Auth Flow

1. User registers at Portal.LUNA.App.
2. User generates an API key for an MCP server in the portal.
3. User configures their AI client with the MCP server URL and API key.
4. AI client sends `Authorization: Bearer {apiKey}` to the MCP server.
5. MCP server validates the key against Portal.LUNA.API.

---

## Template Prompt for Future MCP Server Docker Images

Use the following template as a starting point when creating a new MCP server:

---

### MCP Server Template

**Environment:**
- .NET 10 (`net10.0`)
- Runs as a Docker container (x64/ARM64)
- Exposes HTTP endpoints at `/mcp/{MCP_SERVER_NAME}`

**Requirements:**

1. Accept `Authorization: Bearer {apiKey}` header on all requests.
2. Validate the API key by calling Portal.LUNA.API:
   ```
   GET {PORTAL_URL}/api/api-keys/validate?apiKey={apiKey}
   ```
   Return `401 Unauthorized` if invalid.
3. Retrieve per-user settings from the response (`UserApiKeySettings` key-value pairs).
4. Register MCP tools using `ModelContextProtocol.AspNetCore`:
   ```csharp
   builder.Services.AddMcpServer()
       .WithHttpTransport()
       .WithTools<MyTools>();
   app.MapMcp("/mcp/{MCP_SERVER_NAME}");
   ```
5. For sandbox operations, call Portal.LUNA.API:
   - Create: `POST {PORTAL_URL}/api/sandboxes` with `{ "apiKey": "..." }`
   - Destroy: `DELETE {PORTAL_URL}/api/sandboxes/{sandboxId}` with `X-API-Key: ...` header

**Example Program.cs structure:**

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("portal", client => {
    client.BaseAddress = new Uri(builder.Configuration["PortalUrl"] ?? "http://portal-luna:8080");
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<MyServerTools>();

var app = builder.Build();

// Validate API key middleware
app.Use(async (context, next) => {
    var token = context.Request.Headers["Authorization"]
        .ToString().Replace("Bearer ", "").Trim();
    var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var client = factory.CreateClient("portal");
    var resp = await client.GetAsync($"/api/api-keys/validate?apiKey={Uri.EscapeDataString(token)}");
    if (!resp.IsSuccessStatusCode) { context.Response.StatusCode = 401; return; }
    context.Items["ApiKey"] = token;
    await next();
});

app.MapMcp("/mcp/my-server");
app.Run();
```

**Example Tool class:**

```csharp
[McpServerToolType]
public class MyServerTools
{
    [McpServerTool, Description("Does something useful.")]
    public static Task<string> DoSomething(
        [Description("Parameter description")] string param)
    {
        return Task.FromResult($"Result: {param}");
    }
}
```

**Dockerfile:**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["MyServer.MCP.LUNA/MyServer.MCP.LUNA.csproj", "MyServer.MCP.LUNA/"]
RUN dotnet restore "MyServer.MCP.LUNA/MyServer.MCP.LUNA.csproj"
COPY . .
WORKDIR "/src/MyServer.MCP.LUNA"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyServer.MCP.LUNA.dll"]
```

**Required environment variables:**
- `PortalUrl`: URL of Portal.LUNA.API (e.g. `http://portal-luna:8080`)
