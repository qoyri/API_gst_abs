using Microsoft.AspNetCore.Mvc;

namespace gest_abs.Controllers
{
    [ApiController]
    [Route("api/stats")]
    public class StatsController : ControllerBase
    {
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "API en ligne" });
        }
    }
}