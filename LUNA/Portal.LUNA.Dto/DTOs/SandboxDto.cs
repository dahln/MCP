namespace Portal.LUNA.Dto;

public class SandboxDto
{
    public string Id { get; set; } = string.Empty;
    public string UserApiKeyId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
}
