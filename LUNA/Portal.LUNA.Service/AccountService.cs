using Portal.LUNA.Database;
using Portal.LUNA.Dto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Portal.LUNA.Service;

public class AccountService
{
    private ApplicationDbContext _db { get; }
    private UserManager<IdentityUser> _userManager;
    private SignInManager<IdentityUser> _signInManager;

    public AccountService(ApplicationDbContext applicationDbContext, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        _db = applicationDbContext;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public async Task<int> UserCount()
    {
        return await _db.Users.CountAsync();
    }

    public async Task<Dto.SystemSettings> GetSystemSettings()
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new Database.SystemSetting { RegistrationEnabled = true };
            _db.SystemSettings.Add(settings);
            await _db.SaveChangesAsync();
        }

        return new Dto.SystemSettings
        {
            EmailApiKey = "--- NOT DISPLAYED FOR SECURITY ---",
            SystemEmailAddress = settings.SystemEmailAddress,
            RegistrationEnabled = settings.RegistrationEnabled,
            EmailDomainRestriction = settings.EmailDomainRestriction,
            PortalRootUrl = settings.PortalRootUrl
        };
    }

    public async Task UpdateSystemSettings(Dto.SystemSettings model)
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new Database.SystemSetting();
            _db.SystemSettings.Add(settings);
        }

        settings.EmailApiKey = (model.EmailApiKey?.Trim() == "--- NOT DISPLAYED FOR SECURITY ---")
            ? settings.EmailApiKey
            : model.EmailApiKey;
        settings.SystemEmailAddress = model.SystemEmailAddress;
        settings.RegistrationEnabled = model.RegistrationEnabled;
        settings.EmailDomainRestriction = model.EmailDomainRestriction;
        settings.PortalRootUrl = model.PortalRootUrl?.TrimEnd('/');

        await _db.SaveChangesAsync();
    }

    public async Task<Dto.SearchResponse<Dto.User>> UserSearch(Dto.Search model, string userId)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(model.FilterText))
            query = query.Where(i => i.Email!.ToLower().Contains(model.FilterText.ToLower()));

        query = model.SortDirection == SortDirection.Ascending
            ? query.OrderBy(c => c.Email)
            : query.OrderByDescending(c => c.Email);

        var response = new Dto.SearchResponse<Dto.User>
        {
            Total = await query.CountAsync()
        };

        var data = await query.Skip(model.Page * model.PageSize).Take(model.PageSize).ToListAsync();

        response.Results = data.Select(c => new Dto.User
        {
            Id = c.Id,
            Email = c.Email,
            IsAdministrator = false
        }).ToList();

        foreach (var user in response.Results)
        {
            var identityUser = await _userManager.FindByIdAsync(user.Id);
            if (identityUser != null)
                user.IsAdministrator = await _userManager.IsInRoleAsync(identityUser, "Administrator");
            user.IsSelf = user.Id == userId;
        }

        return response;
    }

    public async Task<bool> AccountAllowRegistrationOperations()
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        return settings?.RegistrationEnabled ?? true;
    }

    public async Task<bool> AccountAllowAllOperations()
    {
        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        if (settings == null) return false;
        return !string.IsNullOrEmpty(settings.EmailApiKey) && !string.IsNullOrEmpty(settings.SystemEmailAddress);
    }

    public async Task DeleteAccount(string userId)
    {
        var userApiKeys = _db.UserApiKeys.Where(x => x.UserId == userId);
        foreach (var key in userApiKeys)
        {
            var kvps = _db.UserApiKeyKvps.Where(k => k.ApiKeyId == key.Id);
            _db.UserApiKeyKvps.RemoveRange(kvps);
        }
        _db.UserApiKeys.RemoveRange(userApiKeys);
        await _db.SaveChangesAsync();

        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
            await _userManager.DeleteAsync(user);
    }

    public async Task<bool> AccountTwoFactorEnabled(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;
        return await _userManager.GetTwoFactorEnabledAsync(user);
    }

    public async Task ToggleUserAdministratorRole(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;
        var isAdmin = await _userManager.IsInRoleAsync(user, "Administrator");
        if (isAdmin)
            await _userManager.RemoveFromRoleAsync(user, "Administrator");
        else
            await _userManager.AddToRoleAsync(user, "Administrator");
    }

    public async Task<List<string>> GeCurrentUserRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return new List<string>();
        var roles = await _userManager.GetRolesAsync(user);
        return roles.ToList();
    }

    public async Task<bool> AccountExistsByEmail(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null;
    }

    public async Task AccountLogout()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<List<string>> Register(string email, string password)
    {
        var results = new List<string>();

        var settings = await _db.SystemSettings.FirstOrDefaultAsync();
        if (settings?.RegistrationEnabled == false)
        {
            results.Add("Registration Disabled");
            return results;
        }

        if (!string.IsNullOrEmpty(settings?.EmailDomainRestriction))
        {
            var validDomains = settings.EmailDomainRestriction.Split(",")
                .Select(d => d.Replace("@", "").Trim().ToLower()).ToList();
            var registrationDomain = email.ToLower().Split("@").LastOrDefault();
            if (!validDomains.Contains(registrationDomain ?? ""))
            {
                results.Add($"Invalid domain. You must use an email ending in: {settings.EmailDomainRestriction}");
                return results;
            }
        }

        var user = new IdentityUser { UserName = email, Email = email };
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            var count = await UserCount();
            if (count == 1)
            {
                var identity = await _userManager.FindByEmailAsync(email);
                if (identity != null)
                    await _userManager.AddToRoleAsync(identity, "Administrator");
            }
            return results;
        }

        foreach (var error in result.Errors)
            results.Add(error.Description);

        return results;
    }
}
