using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portal.LUNA.API.Utility;
using Portal.LUNA.Dto;
using Portal.LUNA.Service;

namespace Portal.LUNA.API.Controllers;

[ApiController]
[Route("api/v1/apikeys")]
[Authorize]
public class ApiKeyController : Controller
{
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyController(ApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyApiKeys()
    {
        string userId = User.GetUserId();
        var keys = await _apiKeyService.GetUserApiKeysAsync(userId);
        return Ok(keys);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetApiKeyById(string id)
    {
        string userId = User.GetUserId();
        var key = await _apiKeyService.GetUserApiKeyByIdAsync(id, userId);
        if (key == null) return NotFound();
        return Ok(key);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateApiKey([FromBody] Dto.UserApiKey model)
    {
        string userId = User.GetUserId();
        try
        {
            var key = await _apiKeyService.GenerateApiKeyAsync(userId, model.ServerId, model.ContainerId);
            return Ok(key);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiKey(string id)
    {
        string userId = User.GetUserId();
        var deleted = await _apiKeyService.DeleteApiKeyAsync(id, userId);
        return deleted ? Ok() : NotFound();
    }

    // --- KVP management ---

    [HttpPost("{apiKeyId}/kvp")]
    public async Task<IActionResult> AddKvp(string apiKeyId, [FromBody] Dto.ApiKeyKvp model)
    {
        string userId = User.GetUserId();
        if (string.IsNullOrEmpty(model.Key) || string.IsNullOrEmpty(model.Value))
            return BadRequest("Key and Value are required.");
        try
        {
            var result = await _apiKeyService.AddKvpAsync(apiKeyId, userId, model);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{apiKeyId}/kvp/{kvpId}")]
    public async Task<IActionResult> UpdateKvp(string apiKeyId, string kvpId, [FromBody] Dto.ApiKeyKvp model)
    {
        string userId = User.GetUserId();
        var updated = await _apiKeyService.UpdateKvpAsync(kvpId, apiKeyId, userId, model);
        return updated ? Ok() : NotFound();
    }

    [HttpDelete("{apiKeyId}/kvp/{kvpId}")]
    public async Task<IActionResult> DeleteKvp(string apiKeyId, string kvpId)
    {
        string userId = User.GetUserId();
        var deleted = await _apiKeyService.DeleteKvpAsync(kvpId, apiKeyId, userId);
        return deleted ? Ok() : NotFound();
    }
}
