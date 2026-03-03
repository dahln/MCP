namespace Portal.LUNA.Dto;

public class CreateSandboxRequestDto
{
    public string ApiKey { get; set; } = string.Empty;
}

public class CreateSandboxResponseDto
{
    public string SandboxId { get; set; } = string.Empty;
}
