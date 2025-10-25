using MatchMaking.Service.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController(IMatchService service) : ControllerBase
{
    private readonly IMatchService _service = service;

    [HttpGet("Ping")]
    public Task<IActionResult> Ping()
    {
        return Task.FromResult<IActionResult>(Ok($"Pong-{Guid.NewGuid()}"));
    }

    [HttpPost("Request")]
    public async Task<IActionResult> SearchMatch([FromHeader] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return BadRequest("UserId required");

        var ok = await _service.RequestMatchAsync(userId);
        if (!ok)
            return StatusCode(429, "Too many requests");

        return NoContent();
    }

    [HttpGet("Matches")]
    public async Task<IActionResult> GetMatches([FromHeader] string userId)
    {
        var match = await _service.GetMatchAsync(userId);
        return match is null ? NotFound() : Ok(match);
    }
}
