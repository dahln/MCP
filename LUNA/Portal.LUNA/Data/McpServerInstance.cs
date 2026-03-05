namespace Portal.LUNA.Data;

public class McpServerInstance
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string AvailableMcpServerId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public int Port { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StoppedAt { get; set; }
    
    public AvailableMcpServer AvailableMcpServer { get; set; } = null!;
}
