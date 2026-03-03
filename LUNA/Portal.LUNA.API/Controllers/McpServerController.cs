using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.API.Utility;
using Portal.LUNA.Dto;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/v1/mcp")]
public class McpServerController : Controller
{
    private readonly McpServerService _mcpService;

    public McpServerController(McpServerService mcpService)
    {
        _mcpService = mcpService;
    }

    // --- Admin: Available MCP Server catalog ---

    [Authorize(Roles = "Administrator")]
    [HttpGet("catalog")]
    public async Task<IActionResult> GetCatalog()
    {
        var servers = await _mcpService.GetAvailableServersAsync();
        return Ok(servers);
    }

    [Authorize(Roles = "Administrator")]
    [HttpGet("catalog/{id}")]
    public async Task<IActionResult> GetCatalogById(string id)
    {
        var server = await _mcpService.GetAvailableServerByIdAsync(id);
        if (server == null) return NotFound();
        return Ok(server);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("catalog")]
    public async Task<IActionResult> CreateCatalogEntry([FromBody] Dto.AvailableMcpServer model)
    {
        if (string.IsNullOrEmpty(model.Name) || string.IsNullOrEmpty(model.DockerImage))
            return BadRequest("Name and DockerImage are required.");
        var result = await _mcpService.CreateAvailableServerAsync(model);
        return Ok(result);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPut("catalog/{id}")]
    public async Task<IActionResult> UpdateCatalogEntry(string id, [FromBody] Dto.AvailableMcpServer model)
    {
        model.Id = id;
        var updated = await _mcpService.UpdateAvailableServerAsync(model);
        return updated ? Ok() : NotFound();
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("catalog/{id}")]
    public async Task<IActionResult> DeleteCatalogEntry(string id)
    {
        var deleted = await _mcpService.DeleteAvailableServerAsync(id);
        return deleted ? Ok() : NotFound();
    }

    // --- Admin: Container management ---

    [Authorize(Roles = "Administrator")]
    [HttpGet("containers")]
    public async Task<IActionResult> ListContainers()
    {
        var containers = await _mcpService.ListAllContainersAsync();
        return Ok(containers);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("containers")]
    public async Task<IActionResult> StartContainer([FromBody] Dto.CreateContainerRequest request)
    {
        try
        {
            var container = await _mcpService.StartContainerAsync(request);
            return Ok(container);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost("containers/{instanceId}/stop")]
    public async Task<IActionResult> StopContainer(string instanceId)
    {
        var stopped = await _mcpService.StopContainerAsync(instanceId);
        return stopped ? Ok() : NotFound();
    }

    [Authorize(Roles = "Administrator")]
    [HttpDelete("containers/{instanceId}")]
    public async Task<IActionResult> DeleteContainer(string instanceId)
    {
        var deleted = await _mcpService.DeleteContainerAsync(instanceId);
        return deleted ? Ok() : NotFound();
    }

    // --- User: View available/running servers ---

    [Authorize]
    [HttpGet("servers")]
    public async Task<IActionResult> GetRunningServers()
    {
        var servers = await _mcpService.GetRunningServersForUsersAsync();
        return Ok(servers);
    }
}
