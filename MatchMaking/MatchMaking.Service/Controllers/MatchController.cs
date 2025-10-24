using Microsoft.AspNetCore.Mvc;

namespace MatchMaking.Service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase
{
    private readonly ILogger<MatchController> _logger;

    public MatchController(ILogger<MatchController> logger)
    {
        _logger = logger;
    }

    [HttpGet("Ping")]
    public Task<IActionResult> Ping()
    {
        _logger.LogInformation("Ping request received.");
        return Task.FromResult<IActionResult>(Ok($"Pong-{Guid.NewGuid()}"));
    }

    [HttpPost("Request")]
    public Task<IActionResult> SearchMatch([FromHeader] string userId)
    {
        _logger.LogInformation("SearchMatch request received for UserId: {UserId}", userId);
        return Task.FromResult<IActionResult>(Ok("Request accepted!"));
    }

    [HttpGet("Matches")]
    public Task<IActionResult> GetMatches([FromHeader] string userId)
    {
        _logger.LogInformation("GetMatches request received for UserId: {UserId}", userId);
        var matches = new
        {
            MatchId = Guid.NewGuid(),
            UserIds = new[] { "user_1", "user_2", "user_3" },
        };
        return Task.FromResult<IActionResult>(Ok(matches));
    }
}
