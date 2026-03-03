using System.ComponentModel.DataAnnotations;

namespace Portal.LUNA.Database
{
    public class SystemSetting
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? EmailApiKey { get; set; }
        public string? SystemEmailAddress { get; set; }
        public bool RegistrationEnabled { get; set; } = true;
        public string? EmailDomainRestriction { get; set; }
        public string? PortalRootUrl { get; set; }
    }

    public class AvailableMcpServer
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string DockerImage { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int ExternalPort { get; set; } = 8080;
    }

    public class McpContainerInstance
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ContainerId { get; set; } = string.Empty;

        [Required]
        public string ContainerName { get; set; } = string.Empty;

        [Required]
        public string ServerId { get; set; } = string.Empty;

        public int HostPort { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserApiKey
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string ServerId { get; set; } = string.Empty;

        [Required]
        public string ApiKey { get; set; } = string.Empty;

        public string? ContainerInstanceId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserApiKeyKvp> KeyValuePairs { get; set; } = new List<UserApiKeyKvp>();
    }

    public class UserApiKeyKvp
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ApiKeyId { get; set; } = string.Empty;

        [Required]
        public string Key { get; set; } = string.Empty;

        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
