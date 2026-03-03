namespace Portal.LUNA.Dto
{
    public class AvailableMcpServer
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DockerImage { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ExternalPort { get; set; }
    }

    public class McpContainer
    {
        public string Id { get; set; } = string.Empty;
        public string ContainerId { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;
        public string? ServerName { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool ManagedByLuna { get; set; }
        public int? HostPort { get; set; }
    }

    public class CreateContainerRequest
    {
        public string ServerId { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
    }

    public class UserApiKey
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string ServerId { get; set; } = string.Empty;
        public string? ServerName { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string? ContainerId { get; set; }
        public string? ContainerName { get; set; }
        public string? McpServerAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ApiKeyKvp> KeyValuePairs { get; set; } = new();
    }

    public class ApiKeyKvp
    {
        public string Id { get; set; } = string.Empty;
        public string ApiKeyId { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
