using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [Authorize]
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
                    // Tráº£ vá» cáº¥u hÃ¬nh máº·c Ä‘á»‹nh náº¿u chÆ°a cÃ³
                    return Ok(new QRSettings());
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi láº¥y cáº¥u hÃ¬nh QR", error = ex.Message });
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
                    // Táº¡o má»›i
                    settings.CreatedAt = DateTime.Now;
                    settings.UpdatedAt = DateTime.Now;
                    _context.QRSettings.Add(settings);
                }
                else
                {
                    // Cáº­p nháº­t
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
                return Ok(new { message = "ÄÃ£ lÆ°u cáº¥u hÃ¬nh QR thÃ nh cÃ´ng" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi lÆ°u cáº¥u hÃ¬nh QR", error = ex.Message });
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
                    return BadRequest(new { message = "ChÆ°a cáº¥u hÃ¬nh QR hoáº·c QR bá»‹ táº¯t" });
                }

                // Kiá»ƒm tra thÃ´ng tin cáº§n thiáº¿t
                if (string.IsNullOrEmpty(settings.BankCode) || 
                    string.IsNullOrEmpty(settings.BankAccountNumber) || 
                    string.IsNullOrEmpty(settings.BankAccountHolder))
                {
                    return BadRequest(new { message = "Thiáº¿u thÃ´ng tin cáº¥u hÃ¬nh QR" });
                }

                return Ok(new { 
                    message = "Cáº¥u hÃ¬nh QR há»£p lá»‡",
                    provider = settings.QRProvider,
                    bankName = settings.BankName,
                    accountHolder = settings.BankAccountHolder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi kiá»ƒm tra cáº¥u hÃ¬nh QR", error = ex.Message });
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
                    return BadRequest(new { message = "ChÆ°a cáº¥u hÃ¬nh QR hoáº·c QR bá»‹ táº¯t" });
                }

                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Sá»‘ tiá»n pháº£i lá»›n hÆ¡n 0" });
                }

                // Chuáº©n bá»‹ thÃ´ng tin Ä‘á»ƒ táº¡o QR
                var qrRequest = new VietQRRequest
                {
                    BankCode = settings.BankCode,
                    AccountNumber = settings.BankAccountNumber,
                    AccountHolder = settings.BankAccountHolder,
                    Amount = request.Amount,
                    Description = GetQRDescription(request.Description, request.OrderId, settings.DefaultDescription)
                };

                // Gá»i VietQR Image API trá»±c tiáº¿p
                var qrImageUrl = "";
                if (settings.QRProvider.ToLower() == "vietqr")
                {
                    // Sá»­ dá»¥ng VietQR Image API vá»›i kÃ­ch thÆ°á»›c lá»›n hÆ¡n
                    var template = !string.IsNullOrEmpty(settings.QRTemplate) ? settings.QRTemplate : "compact";
                    qrImageUrl = $"https://api.vietqr.io/image/{settings.BankCode}-{settings.BankAccountNumber}-{template}.jpg" +
                               $"?accountName={Uri.EscapeDataString(settings.BankAccountHolder)}" +
                               $"&amount={request.Amount}" +
                               $"&size=512";  // ThÃªm kÃ­ch thÆ°á»›c 512x512 Ä‘á»ƒ QR rÃµ nÃ©t hÆ¡n
                    
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
                return StatusCode(500, new { message = "Lá»—i khi táº¡o QR", error = ex.Message });
            }
        }

        // GET: api/QRSettings/generate-url - Táº¡o QR URL Ä‘Æ¡n giáº£n
        [HttpGet("generate-url")]
        public async Task<IActionResult> GenerateQRUrl(decimal amount, string? description = null, string? orderId = null)
        {
            try
            {
                var settings = await _context.QRSettings.FirstOrDefaultAsync();
                
                if (settings == null || !settings.IsEnabled)
                {
                    return BadRequest(new { message = "QR Code chÆ°a Ä‘Æ°á»£c cáº¥u hÃ¬nh hoáº·c bá»‹ táº¯t" });
                }

                // Táº¡o mÃ´ táº£ vá»›i format má»›i
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
                               $"&size=512";  // ThÃªm kÃ­ch thÆ°á»›c 512x512 Ä‘á»ƒ QR rÃµ nÃ©t hÆ¡n
                    
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
                return StatusCode(500, new { message = "Lá»—i khi táº¡o QR URL", error = ex.Message });
            }
        }

        // Helper method Ä‘á»ƒ táº¡o mÃ´ táº£ QR theo format má»›i
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
