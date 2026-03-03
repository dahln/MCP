namespace Portal.LUNA.Models;

public class SandboxDto
{
    public string Id { get; set; } = string.Empty;
    public string UserApiKeyId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DestroyedAt { get; set; }
}

public class CreateSandboxRequestDto
{
    public string ApiKey { get; set; } = string.Empty;
}

public class CreateSandboxResponseDto
{
    public string SandboxId { get; set; } = string.Empty;
}
