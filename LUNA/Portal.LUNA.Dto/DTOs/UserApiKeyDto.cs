namespace Portal.LUNA.Dto;

public class UserApiKeyDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string McpServerId { get; set; } = string.Empty;
    public string McpServerName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public List<UserApiKeySettingDto> Settings { get; set; } = new();
}
