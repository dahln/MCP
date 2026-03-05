namespace Portal.LUNA.Models;

public class McpServerInstanceDto
{
    public string Id { get; set; } = string.Empty;
    public string AvailableMcpServerId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
}
