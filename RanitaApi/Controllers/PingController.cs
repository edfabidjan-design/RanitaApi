using Microsoft.AspNetCore.Mvc;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "Ranita API OK 🚀" });
        }
    }
}