using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/containers")]
[Authorize(Policy = "AdminOnly")]
public class ContainersController : ControllerBase
{
    private readonly IContainerService _service;

    public ContainersController(IContainerService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllContainersAsync());

    [HttpPost("{containerId}/start")]
    public async Task<IActionResult> Start(string containerId)
    {
        await _service.StartContainerAsync(containerId);
        return Ok();
    }

    [HttpPost("{containerId}/stop")]
    public async Task<IActionResult> Stop(string containerId)
    {
        await _service.StopContainerAsync(containerId);
        return Ok();
    }

    [HttpDelete("{containerId}")]
    public async Task<IActionResult> Delete(string containerId)
    {
        await _service.DeleteContainerAsync(containerId);
        return Ok();
    }
}
