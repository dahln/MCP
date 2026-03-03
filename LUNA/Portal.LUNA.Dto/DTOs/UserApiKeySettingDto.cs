namespace Portal.LUNA.Dto;

public class UserApiKeySettingDto
{
    public string Id { get; set; } = string.Empty;
    public string UserApiKeyId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
