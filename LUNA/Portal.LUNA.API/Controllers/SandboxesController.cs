using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.Dto;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/sandboxes")]
public class SandboxesController : ControllerBase
{
    private readonly ISandboxService _service;

    public SandboxesController(ISandboxService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSandboxRequestDto req)
    {
        try
        {
            var result = await _service.CreateAsync(req.ApiKey);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpDelete("{sandboxId}")]
    public async Task<IActionResult> Destroy(string sandboxId, [FromHeader(Name = "X-API-Key")] string apiKey)
    {
        var result = await _service.DestroyAsync(sandboxId, apiKey);
        return result ? Ok() : NotFound();
    }

    [HttpGet("{sandboxId}")]
    public async Task<IActionResult> Get(string sandboxId, [FromHeader(Name = "X-API-Key")] string apiKey)
    {
        var result = await _service.GetAsync(sandboxId, apiKey);
        return result == null ? NotFound() : Ok(result);
    }
}
