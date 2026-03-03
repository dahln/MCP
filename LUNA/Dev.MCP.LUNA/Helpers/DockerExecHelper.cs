using System.Diagnostics;
using System.Text.Json;

namespace Dev.MCP.LUNA;

public static class DockerExecHelper
{
    public static async Task<string> RunAsync(string containerId, string command, string workDir = "/workspace")
    {
        // Validate containerId to prevent injection (alphanumeric and hyphens only)
        if (!System.Text.RegularExpressions.Regex.IsMatch(containerId, @"^[a-zA-Z0-9_\-]{1,128}$"))
            return JsonSerializer.Serialize(new { success = false, stdout = "", stderr = "Invalid container ID.", exitCode = -1 });

        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("exec");
        psi.ArgumentList.Add("-w");
        psi.ArgumentList.Add(workDir);
        psi.ArgumentList.Add(containerId);
        psi.ArgumentList.Add("sh");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);

        using var process = Process.Start(psi);
        if (process == null)
            return JsonSerializer.Serialize(new { success = false, stdout = "", stderr = "Failed to start docker exec.", exitCode = -1 });

        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return JsonSerializer.Serialize(new
        {
            success = process.ExitCode == 0,
            stdout,
            stderr,
            exitCode = process.ExitCode
        });
    }
}
