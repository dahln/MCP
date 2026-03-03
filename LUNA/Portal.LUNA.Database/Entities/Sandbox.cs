namespace Portal.LUNA.Database.Entities;

public class Sandbox
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserApiKeyId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DestroyedAt { get; set; }
    
    public UserApiKey UserApiKey { get; set; } = null!;
}
