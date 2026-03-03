using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/api-keys")]
public class ValidateApiKeyController : ControllerBase
{
    private readonly IUserApiKeyService _service;
    public ValidateApiKeyController(IUserApiKeyService service) => _service = service;

    [HttpGet("validate")]
    public async Task<IActionResult> Validate([FromQuery] string apiKey)
    {
        var key = await _service.GetByApiKeyAsync(apiKey);
        return key != null ? Ok(key) : Unauthorized();
    }
}
