using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QRSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public QRSettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/QRSettings
        [HttpGet]
        public async Task<IActionResult> GetQRSettings()
        {
            try
            {
                var settings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Trả về cấu hình mặc định nếu chưa có
                    return Ok(new QRSettings());
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy cấu hình QR", error = ex.Message });
            }
        }

        // POST: api/QRSettings
        [HttpPost]
        public async Task<IActionResult> SaveQRSettings([FromBody] QRSettings settings)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existingSettings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (existingSettings == null)
                {
                    // Tạo mới
                    settings.CreatedAt = DateTime.Now;
                    settings.UpdatedAt = DateTime.Now;
                    _context.QRSettings.Add(settings);
                }
                else
                {
                    // Cập nhật
                    existingSettings.BankCode = settings.BankCode;
                    existingSettings.BankAccountNumber = settings.BankAccountNumber;
                    existingSettings.BankAccountHolder = settings.BankAccountHolder;
                    existingSettings.BankName = settings.BankName;
                    existingSettings.BankBranch = settings.BankBranch;
                    existingSettings.QRProvider = settings.QRProvider;
                    existingSettings.VietQRClientId = settings.VietQRClientId;
                    existingSettings.VietQRApiKey = settings.VietQRApiKey;
                    existingSettings.VNPayApiKey = settings.VNPayApiKey;
                    existingSettings.VNPaySecretKey = settings.VNPaySecretKey;
                    existingSettings.QRTemplate = settings.QRTemplate;
                    existingSettings.IsEnabled = settings.IsEnabled;
                    existingSettings.DefaultDescription = settings.DefaultDescription;
                    existingSettings.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Đã lưu cấu hình QR thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lưu cấu hình QR", error = ex.Message });
            }
        }

        // GET: api/QRSettings/test
        [HttpGet("test")]
        public async Task<IActionResult> TestQRSettings()
        {
            try
            {
                var settings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (settings == null || !settings.IsEnabled)
                {
                    return BadRequest(new { message = "Chưa cấu hình QR hoặc QR bị tắt" });
                }

                // Kiểm tra thông tin cần thiết
                if (string.IsNullOrEmpty(settings.BankCode) || 
                    string.IsNullOrEmpty(settings.BankAccountNumber) || 
                    string.IsNullOrEmpty(settings.BankAccountHolder))
                {
                    return BadRequest(new { message = "Thiếu thông tin cấu hình QR" });
                }

                return Ok(new { 
                    message = "Cấu hình QR hợp lệ",
                    provider = settings.QRProvider,
                    bankName = settings.BankName,
                    accountHolder = settings.BankAccountHolder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi kiểm tra cấu hình QR", error = ex.Message });
            }
        }

        // POST: api/QRSettings/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQR([FromBody] GenerateQRRequest request)
        {
            try
            {
                var settings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (settings == null || !settings.IsEnabled)
                {
                    return BadRequest(new { message = "Chưa cấu hình QR hoặc QR bị tắt" });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Số tiền phải lớn hơn 0" });
                }

                // Chuẩn bị thông tin để tạo QR
                var qrRequest = new VietQRRequest
                {
                    BankCode = settings.BankCode,
                    AccountNumber = settings.BankAccountNumber,
                    AccountHolder = settings.BankAccountHolder,
                    Amount = request.Amount,
                    Description = GetQRDescription(request.Description, request.OrderId, settings.DefaultDescription)
                };

                // Gọi VietQR Image API trực tiếp
                var qrImageUrl = "";
                if (settings.QRProvider.ToLower() == "vietqr")
                {
                    // Sử dụng VietQR Image API với kích thước lớn hơn
                    var template = !string.IsNullOrEmpty(settings.QRTemplate) ? settings.QRTemplate : "compact";
                    qrImageUrl = $"https://api.vietqr.io/image/{settings.BankCode}-{settings.BankAccountNumber}-{template}.jpg" +
                               $"?accountName={Uri.EscapeDataString(settings.BankAccountHolder)}" +
                               $"&amount={request.Amount}" +
                               $"&size=512";  // Thêm kích thước 512x512 để QR rõ nét hơn
                    
                    if (!string.IsNullOrEmpty(qrRequest.Description))
                    {
                        qrImageUrl += $"&addInfo={Uri.EscapeDataString(qrRequest.Description)}";
                    }
                }

                return Ok(new { 
                    success = true,
                    qrImageUrl = qrImageUrl,
                    amount = request.Amount,
                    description = qrRequest.Description,
                    bankInfo = $"{settings.BankName} - {settings.BankAccountNumber}",
                    accountHolder = settings.BankAccountHolder,
                    provider = settings.QRProvider
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo QR", error = ex.Message });
            }
        }

        // GET: api/QRSettings/generate-url - Tạo QR URL đơn giản
        [HttpGet("generate-url")]
        public async Task<IActionResult> GenerateQRUrl(decimal amount, string? description = null, string? orderId = null)
        {
            try
            {
                var settings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (settings == null || !settings.IsEnabled)
                {
                    return BadRequest(new { message = "QR Code chưa được cấu hình hoặc bị tắt" });
                }

                // Tạo mô tả với format mới
                var qrDescription = "";
                if (!string.IsNullOrEmpty(orderId))
                {
                    qrDescription = $"thanh toan don hang theo hoa don {orderId}";
                }
                else if (!string.IsNullOrEmpty(description))
                {
                    qrDescription = description;
                }
                else
                {
                    qrDescription = settings.DefaultDescription ?? "Thanh toan hoa don";
                }

                var qrImageUrl = "";
                if (settings.QRProvider.ToLower() == "vietqr")
                {
                    var template = !string.IsNullOrEmpty(settings.QRTemplate) ? settings.QRTemplate : "compact";
                    qrImageUrl = $"https://api.vietqr.io/image/{settings.BankCode}-{settings.BankAccountNumber}-{template}.jpg" +
                               $"?accountName={Uri.EscapeDataString(settings.BankAccountHolder)}" +
                               $"&amount={amount}" +
                               $"&size=512";  // Thêm kích thước 512x512 để QR rõ nét hơn
                    
                    if (!string.IsNullOrEmpty(qrDescription))
                    {
                        qrImageUrl += $"&addInfo={Uri.EscapeDataString(qrDescription)}";
                    }
                }

                return Ok(new { 
                    qrImageUrl = qrImageUrl,
                    bankName = settings.BankName,
                    accountNumber = settings.BankAccountNumber,
                    accountHolder = settings.BankAccountHolder,
                    amount = amount,
                    description = qrDescription
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo QR URL", error = ex.Message });
            }
        }

        // Helper method để tạo mô tả QR theo format mới
        private string GetQRDescription(string? description, string? orderId, string? defaultDescription)
        {
            if (!string.IsNullOrEmpty(orderId))
            {
                return $"thanh toan don hang theo hoa don {orderId}";
            }
            else if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            else
            {
                return defaultDescription ?? "Thanh toan hoa don";
            }
        }
    }

    public class GenerateQRRequest
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? OrderId { get; set; }
    }
}