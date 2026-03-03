using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;

namespace Ping.MCP.LUNA.Tools;

[McpServerToolType]
public class PingTools
{
    [McpServerTool, Description("Sends a ping to the specified host or IP address and returns the results.")]
    public static async Task<string> Ping(
        [Description("The hostname, IP address, or URL to ping (e.g. 'google.com', '8.8.8.8', 'https://example.com')")] string target,
        [Description("Number of ping packets to send (default: 4, max: 10)")] int count = 4)
    {
        if (string.IsNullOrWhiteSpace(target))
            return "Error: Target is required.";

        if (count < 1) count = 1;
        if (count > 10) count = 10;

        // Strip protocol if present
        target = target.Replace("https://", "").Replace("http://", "").Split('/')[0];

        try
        {
            var isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)
                       || System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

            string arguments = isLinux
                ? $"-c {count} {target}"
                : $"-n {count} {target}";

            var psi = new ProcessStartInfo
            {
                FileName = "ping",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return "Error: Could not start ping process.";

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderr = await process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            string result = string.IsNullOrEmpty(stdout) ? stderr : stdout;
            return string.IsNullOrEmpty(result) ? "No output from ping command." : result;
        }
        catch (Exception ex)
        {
            return $"Error executing ping: {ex.Message}";
        }
    }
}
