using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System.ComponentModel.DataAnnotations;
using System.Drawing.Printing;

namespace RetailPointBackend.Controllers
{
    [Authorize]
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
        /// Láº¥y cÃ i Ä‘áº·t in áº¥n hiá»‡n táº¡i
        /// </summary>
        /// <returns>Cáº¥u hÃ¬nh in áº¥n</returns>
        [HttpGet]
        public async Task<ActionResult<PrintConfig>> GetConfig()
        {
            try
            {
                var config = await _context.PrintConfigs.FirstOrDefaultAsync();
                if (config == null)
                {
                    // Táº¡o cáº¥u hÃ¬nh máº·c Ä‘á»‹nh náº¿u chÆ°a cÃ³
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
                        BillFooter = "Cáº£m Æ¡n quÃ½ khÃ¡ch!"
                    };
                    _context.PrintConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }
                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lá»—i khi láº¥y cáº¥u hÃ¬nh in", details = ex.Message });
            }
        }

        /// <summary>
        /// Cáº­p nháº­t cÃ i Ä‘áº·t in áº¥n
        /// </summary>
        /// <param name="model">Cáº¥u hÃ¬nh in áº¥n má»›i</param>
        /// <returns>Cáº¥u hÃ¬nh Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t</returns>
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
                    // Cáº­p nháº­t cáº¥u hÃ¬nh hiá»‡n cÃ³
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
                    // Táº¡o má»›i náº¿u chÆ°a cÃ³
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
                        BillFooter = model.BillFooter ?? "Cáº£m Æ¡n quÃ½ khÃ¡ch!"
                    };
                    _context.PrintConfigs.Add(newConfig);
                    existing = newConfig;
                }
                
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lá»—i khi cáº­p nháº­t cáº¥u hÃ¬nh in", details = ex.Message });
            }
        }

        /// <summary>
        /// Láº¥y danh sÃ¡ch mÃ¡y in cÃ³ sáºµn
        /// </summary>
        /// <returns>Danh sÃ¡ch mÃ¡y in</returns>
        [HttpGet("available-printers")]
        public ActionResult GetAvailablePrinters()
        {
            try
            {
                var availablePrinters = new List<string>();
                
                // Chá»‰ cháº¡y trÃªn Windows
                if (OperatingSystem.IsWindows())
                {
                    availablePrinters = System.Drawing.Printing.PrinterSettings.InstalledPrinters
                        .Cast<string>()
                        .ToList();
                }
                else
                {
                    // Cho cÃ¡c platform khÃ¡c, tráº£ vá» danh sÃ¡ch máº·c Ä‘á»‹nh
                    availablePrinters = new List<string> { "Default Printer", "PDF Printer" };
                }
                
                return Ok(new { availablePrinters });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lá»—i khi láº¥y danh sÃ¡ch mÃ¡y in", details = ex.Message });
            }
        }

        /// <summary>
        /// Kiá»ƒm tra káº¿t ná»‘i mÃ¡y in
        /// </summary>
        /// <param name="printerName">TÃªn mÃ¡y in</param>
        /// <returns>Tráº¡ng thÃ¡i káº¿t ná»‘i</returns>
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
                    message = isConnected ? "MÃ¡y in káº¿t ná»‘i thÃ nh cÃ´ng" : "KhÃ´ng tÃ¬m tháº¥y mÃ¡y in"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Lá»—i khi kiá»ƒm tra mÃ¡y in", details = ex.Message });
            }
        }

        /// <summary>
        /// Láº¥y danh sÃ¡ch mÃ¡y in Ä‘Æ°á»£c cÃ i Ä‘áº·t trÃªn há»‡ thá»‘ng
        /// </summary>
        /// <returns>Danh sÃ¡ch tÃªn mÃ¡y in</returns>
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
                return StatusCode(500, new { error = "Lá»—i khi láº¥y danh sÃ¡ch mÃ¡y in", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Model Ä‘á»ƒ cáº­p nháº­t cÃ i Ä‘áº·t in
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
    /// Model Ä‘á»ƒ test káº¿t ná»‘i mÃ¡y in
    /// </summary>
    public class TestPrinterRequest
    {
        [Required]
        [MaxLength(100)]
        public string PrinterName { get; set; } = string.Empty;
    }
}

