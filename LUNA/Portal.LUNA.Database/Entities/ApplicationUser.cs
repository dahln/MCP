using Microsoft.AspNetCore.Identity;

namespace Portal.LUNA.Database.Entities;

public class ApplicationUser : IdentityUser
{
    public bool IsAdmin { get; set; }
    public ICollection<UserApiKey> UserApiKeys { get; set; } = new List<UserApiKey>();
}
