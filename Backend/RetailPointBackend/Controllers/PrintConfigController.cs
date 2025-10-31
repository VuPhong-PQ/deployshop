using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrintConfigController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public PrintConfigController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy cài đặt in ấn hiện tại
        /// </summary>
        /// <returns>Cấu hình in ấn</returns>
        [HttpGet]
        public async Task<ActionResult<PrintConfig>> GetConfig()
        {
            try
            {
                var config = await _context.PrintConfigs.FirstOrDefaultAsync();
                if (config == null)
                {
                    // Tạo cấu hình mặc định nếu chưa có
                    config = new PrintConfig
                    {
                        PrinterName = "Default Printer",
                        PaperSize = "80mm",
                        PrintCopies = 1,
                        AutoPrintBill = true,
                        AutoPrintOnOrder = false,
                        PrintBarcode = true,
                        PrintLogo = false,
                        BillHeader = "RETAIL POINT STORE",
                        BillFooter = "Cảm ơn quý khách!"
                    };
                    _context.PrintConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy cấu hình in", details = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật cài đặt in ấn
        /// </summary>
        /// <param name="model">Cấu hình in ấn mới</param>
        /// <returns>Cấu hình đã được cập nhật</returns>
        [HttpPost]
        [HttpPut]
        public async Task<ActionResult<PrintConfig>> UpdateConfig([FromBody] PrintConfigUpdateModel model)
        {
            try
            {
                // Validate input
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var existing = await _context.PrintConfigs.FirstOrDefaultAsync();
                if (existing != null)
                {
                    // Cập nhật cấu hình hiện có
                    existing.PrinterName = model.PrinterName ?? existing.PrinterName;
                    existing.PaperSize = model.PaperSize ?? existing.PaperSize;
                    existing.PrintCopies = model.PrintCopies;
                    existing.AutoPrintBill = model.AutoPrintBill;
                    existing.AutoPrintOnOrder = model.AutoPrintOnOrder;
                    existing.PrintBarcode = model.PrintBarcode;
                    existing.PrintLogo = model.PrintLogo;
                    existing.BillHeader = model.BillHeader ?? existing.BillHeader;
                    existing.BillFooter = model.BillFooter ?? existing.BillFooter;
                    
                    _context.PrintConfigs.Update(existing);
                }
                else
                {
                    // Tạo mới nếu chưa có
                    var newConfig = new PrintConfig
                    {
                        PrinterName = model.PrinterName ?? "Default Printer",
                        PaperSize = model.PaperSize ?? "80mm",
                        PrintCopies = model.PrintCopies,
                        AutoPrintBill = model.AutoPrintBill,
                        AutoPrintOnOrder = model.AutoPrintOnOrder,
                        PrintBarcode = model.PrintBarcode,
                        PrintLogo = model.PrintLogo,
                        BillHeader = model.BillHeader ?? "RETAIL POINT STORE",
                        BillFooter = model.BillFooter ?? "Cảm ơn quý khách!"
                    };
                    _context.PrintConfigs.Add(newConfig);
                    existing = newConfig;
                }
                
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi cập nhật cấu hình in", details = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách máy in có sẵn
        /// </summary>
        /// <returns>Danh sách máy in</returns>
        [HttpGet("available-printers")]
        public ActionResult GetAvailablePrinters()
        {
            try
            {
                var availablePrinters = new List<string>();
                
                // Chỉ chạy trên Windows
                if (OperatingSystem.IsWindows())
                {
                    availablePrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters
                        .Cast<string>()
                        .ToList();
                }
                else
                {
                    // Cho các platform khác, trả về danh sách mặc định
                    availablePrinters = new List<string> { "Default Printer", "PDF Printer" };
                }
                
                return Ok(new { availablePrinters });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy danh sách máy in", details = ex.Message });
            }
        }

        /// <summary>
        /// Kiểm tra kết nối máy in
        /// </summary>
        /// <param name="printerName">Tên máy in</param>
        /// <returns>Trạng thái kết nối</returns>
        [HttpPost("test-printer")]
        public ActionResult TestPrinter([FromBody] TestPrinterRequest request)
        {
            try
            {
                var isConnected = false;
                
                if (OperatingSystem.IsWindows() && !string.IsNullOrEmpty(request.PrinterName))
                {
                    var availablePrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters
                        .Cast<string>()
                        .ToList();
                    isConnected = availablePrinters.Contains(request.PrinterName);
                }
                
                return Ok(new 
                { 
                    printerName = request.PrinterName, 
                    isConnected,
                    message = isConnected ? "Máy in kết nối thành công" : "Không tìm thấy máy in"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi kiểm tra máy in", details = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách máy in được cài đặt trên hệ thống
        /// </summary>
        /// <returns>Danh sách tên máy in</returns>
        [HttpGet("printers")]
        public IActionResult GetInstalledPrinters()
        {
            try
            {
                var printers = new List<string>();
                
                if (OperatingSystem.IsWindows())
                {
                    printers = System.Drawing.Printing.PrinterSettings.InstalledPrinters
                        .Cast<string>()
                        .ToList();
                }
                
                return Ok(new { printers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lỗi khi lấy danh sách máy in", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Model để cập nhật cài đặt in
    /// </summary>
    public class PrintConfigUpdateModel
    {
        [MaxLength(100)]
        public string? PrinterName { get; set; }
        
        [MaxLength(20)]
        public string? PaperSize { get; set; }
        
        [Range(1, 10)]
        public int PrintCopies { get; set; } = 1;
        
        public bool AutoPrintBill { get; set; } = true;
        public bool AutoPrintOnOrder { get; set; } = false;
        public bool PrintBarcode { get; set; } = true;
        public bool PrintLogo { get; set; } = false;
        
        [MaxLength(200)]
        public string? BillHeader { get; set; }
        
        [MaxLength(200)]
        public string? BillFooter { get; set; }
    }

    /// <summary>
    /// Model để test kết nối máy in
    /// </summary>
    public class TestPrinterRequest
    {
        [Required]
        [MaxLength(100)]
        public string PrinterName { get; set; } = string.Empty;
    }
}
