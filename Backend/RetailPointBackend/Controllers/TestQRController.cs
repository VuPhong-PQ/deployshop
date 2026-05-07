using Microsoft.AspNetCore.Mvc;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestQRController : ControllerBase
    {
        // Test endpoint Äá» táº¡o QR code ÄÆ¡n giáº£n
        [HttpPost("generate")]
        public IActionResult GenerateTestQR([FromBody] TestQRRequest request)
        {
            try
            {
                // Sá»­ dá»¥ng API VietQR Image trá»±c tiáº¿p
                var bankCode = "970436"; // Vietcombank
                var accountNumber = "0091000232791";
                var template = "3bYrdPX";
                
                var qrImageUrl = $"https://api.vietqr.io/image/{bankCode}-{accountNumber}-{template}.jpg" +
                               $"?accountName={Uri.EscapeDataString(request.AccountName)}" +
                               $"&amount={request.Amount}";
                
                if (!string.IsNullOrEmpty(request.Description))
                {
                    qrImageUrl += $"&addInfo={Uri.EscapeDataString(request.Description)}";
                }

                return Ok(new
                {
                    qrImageUrl = qrImageUrl,
                    bankCode = bankCode,
                    accountNumber = accountNumber,
                    accountName = request.AccountName,
                    amount = request.Amount,
                    description = request.Description
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Lá»i táº¡o QR", error = ex.Message });
            }
        }

        // Test endpoint ÄÆ¡n giáº£n
        [HttpGet("simple")]
        public IActionResult GetSimpleQR()
        {
            var qrImageUrl = "https://api.vietqr.io/image/970436-0091000232791-3bYrdPX.jpg?accountName=NGUYEN%20VU%20PHONG&amount=100000";
            
            return Ok(new
            {
                qrImageUrl = qrImageUrl,
                message = "QR code test thÃ nh cÃ´ng"
            });
        }
    }

    public class TestQRRequest
    {
        public string AccountName { get; set; } = "NGUYEN VU PHONG";
        public decimal Amount { get; set; } = 0;
        public string? Description { get; set; }
    }
}