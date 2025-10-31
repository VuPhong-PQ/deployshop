using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VietQRController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VietQRController(AppDbContext context)
        {
            _context = context;
        }

        // Endpoint: POST api/VietQR/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQR([FromBody] VietQRRequest request)
        {
            try
            {
                // Lấy cấu hình QR từ database
                var qrSettings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (qrSettings == null || !qrSettings.IsEnabled)
                {
                    return BadRequest("Chưa cấu hình QR hoặc QR bị tắt");
                }

                // Kiểm tra provider
                if (qrSettings.QRProvider.ToLower() == "vietqr")
                {
                    return await GenerateVietQR(request, qrSettings);
                }
                else if (qrSettings.QRProvider.ToLower() == "vnpayqr")
                {
                    return await GenerateVNPayQR(request, qrSettings);
                }
                else
                {
                    return BadRequest("Provider QR không được hỗ trợ");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo QR: {ex.Message}");
            }
        }

        private async Task<IActionResult> GenerateVietQR(VietQRRequest request, QRSettings settings)
        {
            // Sử dụng thông tin từ cấu hình thay vì request
            var bankCode = !string.IsNullOrEmpty(request.BankCode) ? request.BankCode : settings.BankCode;
            var accountNumber = !string.IsNullOrEmpty(request.AccountNumber) ? request.AccountNumber : settings.BankAccountNumber;
            var accountHolder = !string.IsNullOrEmpty(request.AccountHolder) ? request.AccountHolder : settings.BankAccountHolder;

            var client = new HttpClient();
            
            // Sử dụng API keys từ cấu hình
            var clientId = !string.IsNullOrEmpty(settings.VietQRClientId) ? settings.VietQRClientId : "487346d7-ebb2-4bf0-97a0-13ad72cff87a";
            var apiKey = !string.IsNullOrEmpty(settings.VietQRApiKey) ? settings.VietQRApiKey : "b3a63288-6624-4a0e-abca-c3c0a43390fe";

            client.DefaultRequestHeaders.Add("x-client-id", clientId);
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var payload = new
            {
                accountNo = accountNumber,
                accountName = accountHolder,
                bankCode = bankCode,
                amount = request.Amount,
                addInfo = request.Description ?? settings.DefaultDescription,
                template = settings.QRTemplate
            };

            var response = await client.PostAsJsonAsync("https://api.vietqr.io/v2/generate", payload);
            var json = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest($"Không tạo được QR từ API VietQR: {json}");
            }

            var result = System.Text.Json.JsonSerializer.Deserialize<VietQRApiResponse>(json);
            if (result?.data?.qrDataURL == null)
                return BadRequest("API VietQR trả về dữ liệu không hợp lệ");
            
            var base64 = result.data.qrDataURL.Replace("data:image/png;base64,", "");
            var bytes = Convert.FromBase64String(base64);
            var fileName = $"vietqr_{accountNumber}_{DateTime.Now.Ticks}.png";
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);
            
            var filePath = Path.Combine(uploadsFolder, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            return Ok(new { 
                qrImage = $"/uploads/{fileName}",
                qrDataURL = result.data.qrDataURL,
                provider = "VietQR",
                amount = request.Amount,
                bankInfo = $"{settings.BankName} - {accountNumber}"
            });
        }

        private Task<IActionResult> GenerateVNPayQR(VietQRRequest request, QRSettings settings)
        {
            // Placeholder cho VNPay QR - có thể implement sau
            return Task.FromResult<IActionResult>(Ok(new { 
                message = "VNPay QR chưa được triển khai",
                provider = "VNPayQR",
                amount = request.Amount,
                bankInfo = $"{settings.BankName} - {settings.BankAccountNumber}"
            }));
        }

        // Endpoint: GET api/VietQR/test-qrcode  
        [HttpGet("test-qrcode")]
        public async Task<IActionResult> GenerateTestQrCode()
        {
            try
            {
                var qrSettings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (qrSettings == null)
                {
                    return BadRequest("Chưa cấu hình QR");
                }

                var qrContent = $"Bank: {qrSettings.BankName}\nAccount: {qrSettings.BankAccountNumber}\nHolder: {qrSettings.BankAccountHolder}";
                var base64 = GenerateQrBase64(qrContent);
                
                return Ok(new { 
                    qrBase64 = base64,
                    settings = new {
                        bankName = qrSettings.BankName,
                        provider = qrSettings.QRProvider,
                        isEnabled = qrSettings.IsEnabled
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi tạo QR test: {ex.Message}");
            }
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

    public class VietQRRequest
    {
        public string BankCode { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
    }

    public class VietQRApiResponse
    {
        public VietQRData? data { get; set; }
    }

    public class VietQRData
    {
        public string? qrDataURL { get; set; }
        public string? accountNo { get; set; }
        public string? accountName { get; set; }
        public string? bankCode { get; set; }
        public string? amount { get; set; }
        public string? addInfo { get; set; }
    }
}
