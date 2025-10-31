using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FixEncodingController : ControllerBase
    {
        private readonly FixEncodingService _fixEncodingService;

        public FixEncodingController(FixEncodingService fixEncodingService)
        {
            _fixEncodingService = fixEncodingService;
        }

        [HttpPost("fix-permissions")]
        public async Task<IActionResult> FixPermissionEncoding()
        {
            try
            {
                await _fixEncodingService.FixPermissionEncodingAsync();
                return Ok(new { message = "Permission encoding fixed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error fixing encoding: {ex.Message}" });
            }
        }
    }
}