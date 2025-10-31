using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.Drawing;
using System.IO;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/payment")]
    public class PaymentController : ControllerBase
    {
        // POST: api/payment/test-qr
        [HttpPost("test-qr")]
        public IActionResult GenerateTestQr([FromBody] TestQrRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.AccountNumber) || string.IsNullOrWhiteSpace(request.AccountHolder) || string.IsNullOrWhiteSpace(request.BankName))
            {
                return BadRequest("Thiếu thông tin tài khoản ngân hàng");
            }
            var qrContent = $"Account: {request.AccountNumber}\nName: {request.AccountHolder}\nBank: {request.BankName}";
            var base64 = GenerateQrBase64(qrContent);
            return Ok(new { qrBase64 = base64 });
        }

        private string GenerateQrBase64(string payload)
        {
            using (var qrGenerator = new QRCodeGenerator())
            using (var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q))
            {
                var qrCode = new BitmapByteQRCode(qrData);
                var qrBytes = qrCode.GetGraphic(20);
                return "data:image/png;base64," + Convert.ToBase64String(qrBytes);
            }
        }
    }

    public class TestQrRequest
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
    }
}
