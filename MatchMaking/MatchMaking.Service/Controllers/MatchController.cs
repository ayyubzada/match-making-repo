using MatchMaking.Service.DTOs;
using MatchMaking.Service.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController(IMatchService service) : ControllerBase
{
    private readonly IMatchService _service = service;

    [HttpPost("Request")]
    public async Task<IActionResult> SearchMatch([FromHeader] string userId)
    {
        var result = await _service.RequestMatchAsync(userId);
        return result switch
        {
            RequestMatchResult.BadRequest => BadRequest("Invalid user ID"),
            RequestMatchResult.TooManyRequests => StatusCode(429, "Too many requests"),
            RequestMatchResult.Success => NoContent(),
            _ => StatusCode(500, "Internal server error")
        };
    }

    [HttpGet("Matches")]
    public async Task<IActionResult> GetMatches([FromHeader] string userId)
    {
        var result = await _service.GetMatchAsync(userId);
        return StatusCode(result.StatusCode, result.StatusCode == 200 ? result.Result : result.ErrorMessage);
    }
}
