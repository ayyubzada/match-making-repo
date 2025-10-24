using Microsoft.AspNetCore.Mvc;

namespace MatchMaking.Service.Controllers
{
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
    }
}
