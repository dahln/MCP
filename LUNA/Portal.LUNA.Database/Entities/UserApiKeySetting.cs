namespace Portal.LUNA.Database.Entities;

public class UserApiKeySetting
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserApiKeyId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public UserApiKey UserApiKey { get; set; } = null!;
}
