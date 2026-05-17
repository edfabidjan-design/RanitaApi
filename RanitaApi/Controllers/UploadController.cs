using Microsoft.AspNetCore.Mvc;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace RanitaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Aucun fichier fourni" });

            var cloudinaryUrl = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
            if (string.IsNullOrEmpty(cloudinaryUrl))
                return StatusCode(500, new { error = "Cloudinary non configuré" });

            var cloudinary = new Cloudinary(cloudinaryUrl);
            cloudinary.Api.Secure = true;

            using var stream = file.OpenReadStream();
            var uploadResult = await cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "ranita-settings"
            });

            if (uploadResult.Error != null)
                return StatusCode(500, new { error = uploadResult.Error.Message });

            return Ok(new { url = uploadResult.SecureUrl.ToString() });
        }
    }
}
