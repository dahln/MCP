using McpServer.Models;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace McpServer.Middleware;

public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly string _apiKey;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<AuthSettings> authOptions)
    {
        _next = next;
        _apiKey = authOptions.Value.ApiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey) ||
            !ConstantTimeEquals(providedKey.ToString(), _apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized: invalid or missing API key.");
            return;
        }

        await _next(context);
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        var aBytes = Encoding.UTF8.GetBytes(a);
        var bBytes = Encoding.UTF8.GetBytes(b);
        return CryptographicOperations.FixedTimeEquals(aBytes, bBytes);
    }
}
