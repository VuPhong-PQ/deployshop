using Microsoft.AspNetCore.Mvc;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        [HttpGet("tax")]
        public IActionResult GetTax()
        {
            return Ok(new { taxRate = 10, taxName = "VAT" });
        }

        [HttpGet("payment")]
        public IActionResult GetPayment()
        {
            return Ok(new { paymentMethod = "BankTransfer", bank = "Vietcombank" });
        }

        [HttpGet("store")]
        public IActionResult GetStore()
        {
            return Ok(new { storeName = "Cửa hàng ABC", phone = "0123456789", email = "abc@store.com" });
        }

        [HttpGet("print")]
        public IActionResult GetPrint()
        {
            return Ok(new { printer = "POS-58", status = "ready" });
        }
    }
}
