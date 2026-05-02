using Microsoft.AspNetCore.Mvc;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new List<object>());
        }
    }
}