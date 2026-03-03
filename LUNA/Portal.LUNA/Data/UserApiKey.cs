namespace Portal.LUNA.Data;

public class UserApiKey
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string McpServerId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    
    public ApplicationUser User { get; set; } = null!;
    public AvailableMcpServer McpServer { get; set; } = null!;
    public ICollection<UserApiKeySetting> Settings { get; set; } = new List<UserApiKeySetting>();
    public ICollection<Sandbox> Sandboxes { get; set; } = new List<Sandbox>();
}
