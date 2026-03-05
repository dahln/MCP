namespace Portal.LUNA.Data;

public class AvailableMcpServer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string DockerImage { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ICollection<UserApiKey> UserApiKeys { get; set; } = new List<UserApiKey>();
    public ICollection<McpServerInstance> Instances { get; set; } = new List<McpServerInstance>();
}
