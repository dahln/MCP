using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.Dto;
using Portal.LUNA.Service;
using System.Security.Claims;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/api-keys")]
[Authorize]
public class UserApiKeysController : ControllerBase
{
    private readonly IUserApiKeyService _service;

    public UserApiKeysController(IUserApiKeyService service)
    {
        _service = service;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> GetMyKeys() => Ok(await _service.GetByUserAsync(GetUserId()));

    [HttpPost("generate/{mcpServerId}")]
    public async Task<IActionResult> Generate(string mcpServerId) =>
        Ok(await _service.GenerateAsync(GetUserId(), mcpServerId));

    [HttpDelete("{id}/revoke")]
    public async Task<IActionResult> Revoke(string id)
    {
        var result = await _service.RevokeAsync(id, GetUserId());
        return result ? Ok() : NotFound();
    }

    [HttpPut("{id}/settings")]
    public async Task<IActionResult> UpsertSetting(string id, [FromBody] UserApiKeySettingDto dto)
    {
        var result = await _service.UpsertSettingAsync(id, GetUserId(), dto.Key, dto.Value);
        return Ok(result);
    }

    [HttpDelete("settings/{settingId}")]
    public async Task<IActionResult> DeleteSetting(string settingId)
    {
        var result = await _service.DeleteSettingAsync(settingId, GetUserId());
        return result ? Ok() : NotFound();
    }
}
