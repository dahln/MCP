using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("portal", client =>
{
    var portalUrl = builder.Configuration["PortalUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(portalUrl);
});

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<PingTools>();

var app = builder.Build();

// API key middleware
app.Use(async (context, next) =>
{
    if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Missing Authorization header.");
        return;
    }
    var token = authHeader.ToString();
    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        token = token["Bearer ".Length..].Trim();

    var factory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var portalClient = factory.CreateClient("portal");
    var response = await portalClient.GetAsync($"/api/api-keys/validate?apiKey={Uri.EscapeDataString(token)}");
    if (!response.IsSuccessStatusCode)
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Invalid API key.");
        return;
    }
    await next();
});

app.MapMcp("/mcp/ping");

app.Run();

[McpServerToolType]
public class PingTools
{
    [McpServerTool, Description("Ping a target host or URL. Returns ping output, success status, and exit code.")]
    public static async Task<string> Ping(
        [Description("The target host or IP to ping (e.g. '8.8.8.8' or 'example.com')")] string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return JsonSerializer.Serialize(new { success = false, output = "Target is required.", exitCode = -1 });

        // Validate target to prevent command injection: allow only hostname/IP characters
        if (!System.Text.RegularExpressions.Regex.IsMatch(target, @"^[a-zA-Z0-9.\-]{1,253}$"))
            return JsonSerializer.Serialize(new { success = false, output = "Invalid target. Only hostnames and IP addresses are allowed.", exitCode = -1 });

        var processInfo = new ProcessStartInfo
        {
            FileName = "ping",
            Arguments = OperatingSystem.IsLinux() ? $"-c 4 {target}" : $"-n 4 {target}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
            return JsonSerializer.Serialize(new { success = false, output = "Failed to start ping.", exitCode = -1 });

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return JsonSerializer.Serialize(new
        {
            success = process.ExitCode == 0,
            output = stdout + stderr,
            exitCode = process.ExitCode
        });
    }
}
