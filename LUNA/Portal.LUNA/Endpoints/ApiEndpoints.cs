using Portal.LUNA.Models;
using Portal.LUNA.Services;

namespace Portal.LUNA.Endpoints;

public static class ApiEndpoints
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        // MCP servers call this to validate an API key
        app.MapGet("/api/api-keys/validate", async (string apiKey, IUserApiKeyService svc) =>
        {
            var key = await svc.GetByApiKeyAsync(apiKey);
            return key != null ? Results.Ok(key) : Results.Unauthorized();
        });

        // MCP servers call these to manage sandboxes
        app.MapPost("/api/sandboxes", async (CreateSandboxRequestDto req, ISandboxService svc) =>
        {
            try
            {
                var result = await svc.CreateAsync(req.ApiKey);
                return Results.Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });

        app.MapDelete("/api/sandboxes/{sandboxId}", async (string sandboxId, HttpContext ctx, ISandboxService svc) =>
        {
            var apiKey = ctx.Request.Headers["X-API-Key"].ToString();
            var result = await svc.DestroyAsync(sandboxId, apiKey);
            return result ? Results.Ok() : Results.NotFound();
        });

        app.MapGet("/api/sandboxes/{sandboxId}", async (string sandboxId, HttpContext ctx, ISandboxService svc) =>
        {
            var apiKey = ctx.Request.Headers["X-API-Key"].ToString();
            var result = await svc.GetAsync(sandboxId, apiKey);
            return result != null ? Results.Ok(result) : Results.NotFound();
        });
    }
}
