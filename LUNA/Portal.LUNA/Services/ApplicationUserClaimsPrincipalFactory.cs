using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Portal.LUNA.Data;
using System.Security.Claims;

namespace Portal.LUNA.Services;

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("IsAdmin", user.IsAdmin.ToString().ToLower()));
        return identity;
    }
}
