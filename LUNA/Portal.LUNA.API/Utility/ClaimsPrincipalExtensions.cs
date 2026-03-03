using System.Security.Claims;

namespace Portal.LUNA.API.Utility;

public static class ClaimsPrincipalExtensions
{
    public static string GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}
