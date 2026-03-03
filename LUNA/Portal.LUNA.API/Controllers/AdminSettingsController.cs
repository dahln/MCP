using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.Dto;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = "AdminOnly")]
public class AdminSettingsController : ControllerBase
{
    private readonly IAdminSettingService _service;

    public AdminSettingsController(IAdminSettingService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] AdminSettingDto dto)
    {
        await _service.SetValueAsync(dto.Key, dto.Value);
        return Ok();
    }
}
