using Microsoft.AspNetCore.Mvc;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("ping")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("pong");
        }
    }
}