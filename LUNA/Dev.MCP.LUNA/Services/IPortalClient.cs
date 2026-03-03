using Portal.LUNA.Dto;

namespace Dev.MCP.LUNA;

public interface IPortalClient
{
    Task<CreateSandboxResponseDto> CreateSandboxAsync(string apiKey);
    Task DestroySandboxAsync(string sandboxId, string apiKey);
    Task<UserApiKeyDto?> ValidateApiKeyAsync(string apiKey);
}
