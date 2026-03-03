using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using Portal.LUNA.Dto;
using System.Security.Claims;

namespace Portal.LUNA.App.Services;

public class AuthenticationManager : AuthenticationStateProvider
{
    private readonly API _api;
    private bool _authenticated = false;
    private readonly ClaimsPrincipal Unauthenticated = new(new ClaimsIdentity());

    public AuthenticationManager(API api)
    {
        _api = api;
    }

    public async Task<AuthenticationResponse> RegisterAsync(string email, string password)
    {
        try
        {
            var content = new { email, password };
            var result = await _api.PostAsync("api/v1/account/register", content, true, false);
            return new AuthenticationResponse { Succeeded = result };
        }
        catch { }
        return new AuthenticationResponse { Succeeded = false, ErrorList = ["An unknown error prevented registration."] };
    }

    public async Task<AuthenticationResponse> LoginAsync(string email, string password, string? twoFactorCode, string? twoFactorRecoveryCode)
    {
        try
        {
            var content = new { email, password, twoFactorCode, twoFactorRecoveryCode };
            var response = await _api.SendAsync(HttpMethod.Post, "login?useCookies=true", true, content);
            if (response.IsSuccessStatusCode)
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return new AuthenticationResponse { Succeeded = true };
            }
            else
            {
                string contents = await response.Content.ReadAsStringAsync();
                var error = JsonConvert.DeserializeObject<LoginResponse>(contents);
                if (error?.Detail == "RequiresTwoFactor")
                    return new AuthenticationResponse { Succeeded = false, Prompt2FA = true };
            }
        }
        catch { }
        return new AuthenticationResponse { Succeeded = false, ErrorList = ["Invalid email and/or password."] };
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _authenticated = false;
        var user = Unauthenticated;

        try
        {
            var userResponse = await _api.GetAsync<UserInfo>("manage/info", false, true, false);
            var userRoles = await _api.GetAsync<List<string>>("api/v1/account/roles", false, false, false);

            if (userResponse != null)
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, userResponse.Email),
                    new(ClaimTypes.Email, userResponse.Email)
                };

                if (userRoles != null)
                    claims.AddRange(userRoles.Select(r => new Claim(ClaimTypes.Role, r)));

                claims.AddRange(userResponse.Claims
                    .Where(c => c.Key != ClaimTypes.Name && c.Key != ClaimTypes.Email)
                    .Select(c => new Claim(c.Key, c.Value)));

                user = new ClaimsPrincipal(new ClaimsIdentity(claims, nameof(AuthenticationManager)));
                _authenticated = true;
            }
        }
        catch { }

        return new AuthenticationState(user);
    }

    public async Task LogoutAsync()
    {
        await _api.GetAsync("api/v1/account/logout");
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task<bool> CheckAuthenticatedAsync()
    {
        await GetAuthenticationStateAsync();
        return _authenticated;
    }
}
