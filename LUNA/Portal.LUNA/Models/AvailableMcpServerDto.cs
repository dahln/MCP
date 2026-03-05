namespace Portal.LUNA.Models;

public class AvailableMcpServerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DockerImage { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public List<McpServerInstanceDto> RunningInstances { get; set; } = new();
}
