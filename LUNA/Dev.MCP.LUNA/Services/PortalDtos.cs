namespace Dev.MCP.LUNA;

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

public class UserApiKeySettingDto
{
    public string Id { get; set; } = string.Empty;
    public string UserApiKeyId { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateSandboxRequestDto
{
    public string ApiKey { get; set; } = string.Empty;
}

public class CreateSandboxResponseDto
{
    public string SandboxId { get; set; } = string.Empty;
}
