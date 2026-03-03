using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.API.Utility;
using Portal.LUNA.Dto;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
public class AccountController : Controller
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpPost]
    [Route("api/v1/account/register")]
    public async Task<IActionResult> Register([FromBody] Dto.RegisterRequest model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault());

        var response = await _accountService.Register(model.Email, model.Password);
        return response.Count == 0 ? Ok() : BadRequest(response.FirstOrDefault());
    }

    [Authorize]
    [HttpGet]
    [Route("api/v1/account/logout")]
    public async Task<IActionResult> AccountLogout()
    {
        await _accountService.AccountLogout();
        return Ok();
    }

    [Authorize]
    [HttpPost]
    [Route("api/v1/account/exists")]
    public async Task<IActionResult> AccountExistsByEmail([FromBody] AccountEmail model)
    {
        var exists = await _accountService.AccountExistsByEmail(model.Email);
        return Ok(exists);
    }

    [Authorize]
    [HttpDelete]
    [Route("api/v1/account")]
    public async Task<IActionResult> DeleteAccount()
    {
        string userId = User.GetUserId();
        await _accountService.DeleteAccount(userId);
        return Ok();
    }

    [HttpGet]
    [Route("api/v1/account/operations")]
    public async Task<IActionResult> AccountAllowAllOperations()
    {
        var allow = await _accountService.AccountAllowAllOperations();
        return Ok(allow);
    }

    [HttpGet]
    [Route("api/v1/account/operations/registration")]
    public async Task<IActionResult> AccountAllowRegistrationOperations()
    {
        var allow = await _accountService.AccountAllowRegistrationOperations();
        return Ok(allow);
    }

    [Authorize]
    [HttpGet]
    [Route("api/v1/account/roles")]
    public async Task<IActionResult> GetCurrentUserRoles()
    {
        string userId = User.GetUserId();
        var roles = await _accountService.GeCurrentUserRoles(userId);
        return Ok(roles);
    }

    [HttpGet]
    [Route("api/v1/account/2fa")]
    public async Task<IActionResult> AccountTwoFactorEnabled()
    {
        string userId = User.GetUserId();
        var enabled = await _accountService.AccountTwoFactorEnabled(userId);
        return Ok(enabled);
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    [Route("api/v1/user/{userId}/role/administrator")]
    public async Task<IActionResult> ToggleUserAdministratorRole(string userId)
    {
        string currentUserId = User.GetUserId();
        if (currentUserId == userId)
            return BadRequest("You cannot toggle your own administrative role");
        await _accountService.ToggleUserAdministratorRole(userId);
        return Ok();
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete]
    [Route("api/v1/user/{userId}")]
    public async Task<IActionResult> DeleteUserAsAdministrator(string userId)
    {
        string currentUserId = User.GetUserId();
        if (currentUserId == userId)
            return BadRequest("Cannot delete this account.");
        await _accountService.DeleteAccount(userId);
        return Ok();
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [Route("api/v1/users")]
    public async Task<IActionResult> UserSearch([FromBody] Dto.Search model)
    {
        string userId = User.GetUserId();
        var response = await _accountService.UserSearch(model, userId);
        return Ok(response);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut]
    [Route("api/v1/settings")]
    public async Task<IActionResult> UpdateSystemSettings([FromBody] Dto.SystemSettings model)
    {
        await _accountService.UpdateSystemSettings(model);
        return Ok();
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet]
    [Route("api/v1/settings")]
    public async Task<IActionResult> GetSystemSettings()
    {
        var response = await _accountService.GetSystemSettings();
        return Ok(response);
    }
}
