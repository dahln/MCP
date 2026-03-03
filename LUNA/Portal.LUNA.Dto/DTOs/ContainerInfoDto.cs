namespace Portal.LUNA.Dto;

public class ContainerInfoDto
{
    public string ContainerId { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? McpServerName { get; set; }
    public string? SandboxId { get; set; }
    public bool ManagedByPortal { get; set; }
}
