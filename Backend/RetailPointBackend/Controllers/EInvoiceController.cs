using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class EInvoiceController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IEInvoiceService _eInvoiceService;

        public EInvoiceController(AppDbContext context, IEInvoiceService eInvoiceService)
        {
            _context = context;
            _eInvoiceService = eInvoiceService;
        }

        // GET: api/EInvoice
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EInvoice>>> GetEInvoices(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            var query = _context.EInvoices
                .Include(e => e.Items)
                .Include(e => e.Order)
                .Include(e => e.Staff)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(e => e.Status == status);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(e => e.IssueDate >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(e => e.IssueDate <= toDate.Value);
            }

            var totalCount = await query.CountAsync();
            var invoices = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                data = invoices,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/EInvoice/5
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetEInvoice(int id)
        {
            var eInvoice = await _context.EInvoices
                .Include(e => e.Items!)
                    .ThenInclude(i => i.Product)
                .Include(e => e.Order)
                    .ThenInclude(o => o!.Customer)
                .Include(e => e.Staff)
                .FirstOrDefaultAsync(e => e.EInvoiceId == id);

            if (eInvoice == null)
            {
                return NotFound();
            }

            Console.WriteLine($"GetEInvoice {id}: Items count = {eInvoice.Items?.Count ?? 0}");
            if (eInvoice.Items != null)
            {
                foreach (var item in eInvoice.Items)
                {
                    Console.WriteLine($"  Item: {item.ItemName}, Qty: {item.Quantity}, Price: {item.UnitPrice}");
                }
            }

            // Tráº£ vá» DTO Ä‘á»ƒ trÃ¡nh circular reference
            var result = new
            {
                eInvoiceId = eInvoice.EInvoiceId,
                invoiceNumber = eInvoice.InvoiceNumber,
                invoiceTemplate = eInvoice.InvoiceTemplate,
                invoiceSymbol = eInvoice.InvoiceSymbol,
                issueDate = eInvoice.IssueDate,
                sellerTaxCode = eInvoice.SellerTaxCode,
                sellerName = eInvoice.SellerName,
                sellerAddress = eInvoice.SellerAddress,
                buyerName = eInvoice.BuyerName,
                buyerTaxCode = eInvoice.BuyerTaxCode,
                buyerAddress = eInvoice.BuyerAddress,
                subTotal = eInvoice.SubTotal,
                taxAmount = eInvoice.TaxAmount,
                discountAmount = eInvoice.DiscountAmount,
                totalAmount = eInvoice.TotalAmount,
                status = eInvoice.Status,
                paymentMethod = eInvoice.PaymentMethod,
                notes = eInvoice.Notes,
                orderId = eInvoice.OrderId,
                items = eInvoice.Items?.Select(item => new
                {
                    eInvoiceItemId = item.EInvoiceItemId,
                    lineNumber = item.LineNumber,
                    itemName = item.ItemName,
                    unit = item.Unit,
                    quantity = item.Quantity,
                    unitPrice = item.UnitPrice,
                    lineTotal = item.LineTotal,
                    taxRate = item.TaxRate,
                    taxAmount = item.TaxAmount,
                    totalAmount = item.TotalAmount,
                    discountAmount = item.DiscountAmount,
                    productId = item.ProductId
                }).ToList(),
                customer = eInvoice.Order?.Customer != null ? new
                {
                    customerId = eInvoice.Order.Customer.CustomerId,
                    name = eInvoice.Order.Customer.HoTen,
                    phone = eInvoice.Order.Customer.SoDienThoai,
                    address = eInvoice.Order.Customer.DiaChi
                } : null
            };

            return result;
        }

        // POST: api/EInvoice/create-from-order
        [HttpPost("create-from-order")]
        public async Task<ActionResult<EInvoice>> CreateFromOrder([FromBody] CreateEInvoiceFromOrderRequest request)
        {
            try
            {
                // Láº¥y thÃ´ng tin Ä‘Æ¡n hÃ ng
                var order = await _context.Orders
                    .Include(o => o.Items)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.Customer)
                    .Include(o => o.Staff)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    return NotFound("KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng");
                }

                Console.WriteLine($"Found order: {order.OrderId}, Items count: {order.Items?.Count ?? 0}");
                
                if (order.Items == null || !order.Items.Any())
                {
                    return BadRequest("ÄÆ¡n hÃ ng khÃ´ng cÃ³ sáº£n pháº©m nÃ o");
                }

                // Kiá»ƒm tra Ä‘Ã£ cÃ³ hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­ chÆ°a
                var existingInvoice = await _context.EInvoices
                    .FirstOrDefaultAsync(e => e.OrderId == request.OrderId);

                if (existingInvoice != null)
                {
                    return BadRequest("ÄÆ¡n hÃ ng nÃ y Ä‘Ã£ cÃ³ hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­");
                }

                // Láº¥y cáº¥u hÃ¬nh hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­
                var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
                if (config == null)
                {
                    // Táº¡o cáº¥u hÃ¬nh máº·c Ä‘á»‹nh cho testing
                    config = new EInvoiceConfig
                    {
                        IsEnabled = true,
                        Provider = "VNPT",
                        ApiUrl = "http://101.53.9.76:8080",
                        Username = "admin",
                        Password = "123456",
                        CompanyTaxCode = "0123456789",
                        CompanyName = "CÃ´ng ty TNHH ABC",
                        CompanyAddress = "HÃ  Ná»™i",
                        CompanyPhone = "0123456789",
                        CompanyEmail = "admin@company.com",
                        DefaultTemplate = "1/E-HOA_DON",
                        DefaultSymbol = "E23TEA"
                    };
                    _context.EInvoiceConfigs.Add(config);
                    await _context.SaveChangesAsync();
                }
                
                if (!config.IsEnabled)
                {
                    return BadRequest("HÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­ Ä‘Ã£ bá»‹ táº¯t. Vui lÃ²ng vÃ o cáº¥u hÃ¬nh Ä‘á»ƒ báº­t láº¡i.");
                }

                // Táº¡o sá»‘ hÃ³a Ä‘Æ¡n má»›i
                var invoiceNumber = await GenerateInvoiceNumber();

                // Táº¡o hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­
                var eInvoice = new EInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    InvoiceTemplate = config.DefaultTemplate,
                    InvoiceSymbol = config.DefaultSymbol,
                    IssueDate = DateTime.Now,
                    
                    // ThÃ´ng tin ngÆ°á»i bÃ¡n (tá»« cáº¥u hÃ¬nh)
                    SellerTaxCode = config.CompanyTaxCode ?? "",
                    SellerName = config.CompanyName ?? "",
                    SellerAddress = config.CompanyAddress,
                    SellerPhone = config.CompanyPhone,
                    SellerEmail = config.CompanyEmail,
                    SellerBankAccount = config.CompanyBankAccount,
                    SellerBankName = config.CompanyBankName,
                    
                    // ThÃ´ng tin ngÆ°á»i mua
                    BuyerTaxCode = request.BuyerTaxCode,
                    BuyerName = request.BuyerName ?? order.CustomerName ?? order.Customer?.HoTen,
                    BuyerAddress = request.BuyerAddress ?? order.Customer?.DiaChi,
                    BuyerPhone = request.BuyerPhone ?? order.Customer?.SoDienThoai,
                    BuyerEmail = request.BuyerEmail ?? order.Customer?.Email,
                    
                    // ThÃ´ng tin tiá»n
                    SubTotal = order.SubTotal,
                    TaxAmount = order.TaxAmount,
                    TotalAmount = order.TotalAmount,
                    DiscountAmount = order.DiscountAmount,
                    
                    // ThÃ´ng tin khÃ¡c
                    PaymentMethod = order.PaymentMethod,
                    Notes = request.Notes,
                    OrderId = order.OrderId,
                    StaffId = order.StaffId,
                    Status = "draft"
                };

                _context.EInvoices.Add(eInvoice);
                await _context.SaveChangesAsync();

                // Táº¡o chi tiáº¿t hÃ³a Ä‘Æ¡n
                var lineNumber = 1;
                foreach (var orderItem in order.Items)
                {
                    Console.WriteLine($"Processing OrderItem: {orderItem.OrderItemId}, ProductId: {orderItem.ProductId}, ProductName: {orderItem.ProductName}, Quantity: {orderItem.Quantity}, Price: {orderItem.Price}");
                    Console.WriteLine($"Product info: {orderItem.Product?.Name ?? "NULL"}, Barcode: {orderItem.Product?.Barcode ?? "NULL"}");
                    
                    var eInvoiceItem = new EInvoiceItem
                    {
                        EInvoiceId = eInvoice.EInvoiceId,
                        LineNumber = lineNumber++,
                        ItemCode = orderItem.Product?.Barcode ?? orderItem.ProductId.ToString(),
                        ItemName = orderItem.Product?.Name ?? orderItem.ProductName ?? "Sáº£n pháº©m",
                        Unit = orderItem.Product?.Unit ?? "CÃ¡i",
                        Quantity = orderItem.Quantity,
                        UnitPrice = orderItem.Price,
                        LineTotal = orderItem.Quantity * orderItem.Price,
                        TaxRate = config.DefaultTaxRate ?? "10%",
                        TaxAmount = orderItem.Quantity * orderItem.Price * GetTaxRateValue(config.DefaultTaxRate ?? "10%") / 100,
                        TotalAmount = orderItem.TotalPrice,
                        ProductId = orderItem.ProductId,
                        OrderItemId = orderItem.OrderItemId
                    };

                    Console.WriteLine($"Created EInvoiceItem: ItemCode={eInvoiceItem.ItemCode}, ItemName={eInvoiceItem.ItemName}, Quantity={eInvoiceItem.Quantity}, UnitPrice={eInvoiceItem.UnitPrice}");
                    _context.EInvoiceItems.Add(eInvoiceItem);
                }

                await _context.SaveChangesAsync();

                // Tráº£ vá» thÃ´ng tin cÆ¡ báº£n cá»§a hÃ³a Ä‘Æ¡n vá»«a táº¡o
                return Ok(new { 
                    success = true,
                    eInvoiceId = eInvoice.EInvoiceId,
                    invoiceNumber = eInvoice.InvoiceNumber,
                    status = eInvoice.Status,
                    message = "HÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­ Ä‘Ã£ Ä‘Æ°á»£c táº¡o thÃ nh cÃ´ng"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lá»—i táº¡o hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­: {ex.Message}");
            }
        }

        // POST: api/EInvoice/issue/{id}
        [HttpPost("issue/{id}")]
        public async Task<ActionResult<EInvoice>> IssueEInvoice(int id)
        {
            try
            {
                var eInvoice = await _context.EInvoices
                    .Include(e => e.Items)
                    .FirstOrDefaultAsync(e => e.EInvoiceId == id);

                if (eInvoice == null)
                {
                    return NotFound();
                }

                if (eInvoice.Status != "draft")
                {
                    return BadRequest("Chá»‰ cÃ³ thá»ƒ phÃ¡t hÃ nh hÃ³a Ä‘Æ¡n á»Ÿ tráº¡ng thÃ¡i nhÃ¡p");
                }

                // Láº¥y cáº¥u hÃ¬nh
                var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
                if (config == null || !config.IsEnabled)
                {
                    return BadRequest("ChÆ°a cáº¥u hÃ¬nh hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­");
                }

                // Gá»i API VNPT Ä‘á»ƒ phÃ¡t hÃ nh hÃ³a Ä‘Æ¡n
                var apiResponse = await _eInvoiceService.IssueInvoiceAsync(eInvoice, config);
                
                if (apiResponse.Success)
                {
                    eInvoice.Status = "issued";
                    eInvoice.IssuedAt = DateTime.Now;
                    eInvoice.TransactionUuid = apiResponse.TransactionUuid;
                    eInvoice.InvoiceAuthCode = apiResponse.InvoiceAuthCode;
                    eInvoice.UpdatedAt = DateTime.Now;
                    eInvoice.ErrorMessage = null;
                }
                else
                {
                    eInvoice.Status = "failed";
                    eInvoice.ErrorMessage = apiResponse.ErrorMessage;
                    eInvoice.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(eInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lá»—i phÃ¡t hÃ nh hÃ³a Ä‘Æ¡n: {ex.Message}");
            }
        }

        // POST: api/EInvoice/cancel/{id}
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<EInvoice>> CancelEInvoice(int id, [FromBody] CancelEInvoiceRequest request)
        {
            try
            {
                var eInvoice = await _context.EInvoices
                    .FirstOrDefaultAsync(e => e.EInvoiceId == id);

                if (eInvoice == null)
                {
                    return NotFound();
                }

                if (eInvoice.Status != "issued")
                {
                    return BadRequest("Chá»‰ cÃ³ thá»ƒ há»§y hÃ³a Ä‘Æ¡n Ä‘Ã£ phÃ¡t hÃ nh");
                }

                // Láº¥y cáº¥u hÃ¬nh
                var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
                if (config == null || !config.IsEnabled)
                {
                    return BadRequest("ChÆ°a cáº¥u hÃ¬nh hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­");
                }

                // Gá»i API VNPT Ä‘á»ƒ há»§y hÃ³a Ä‘Æ¡n
                var apiResponse = await _eInvoiceService.CancelInvoiceAsync(eInvoice, config, request.Reason);
                
                if (apiResponse.Success)
                {
                    eInvoice.Status = "cancelled";
                    eInvoice.CancelledAt = DateTime.Now;
                    eInvoice.CancelReason = request.Reason;
                    eInvoice.UpdatedAt = DateTime.Now;
                    eInvoice.ErrorMessage = null;
                }
                else
                {
                    eInvoice.ErrorMessage = apiResponse.ErrorMessage;
                    eInvoice.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                return Ok(eInvoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lá»—i há»§y hÃ³a Ä‘Æ¡n: {ex.Message}");
            }
        }

        // GET: api/EInvoice/config
        [HttpGet("config")]
        public async Task<ActionResult<EInvoiceConfig>> GetConfig()
        {
            var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                // Táº¡o cáº¥u hÃ¬nh máº·c Ä‘á»‹nh
                config = new EInvoiceConfig
                {
                    IsEnabled = false,
                    Provider = "VNPT",
                    CompanyTaxCode = "",
                    CompanyName = ""
                };
                _context.EInvoiceConfigs.Add(config);
                await _context.SaveChangesAsync();
            }
            return Ok(config);
        }

        // PUT: api/EInvoice/config
        [HttpPut("config")]
        public async Task<ActionResult<EInvoiceConfig>> UpdateConfig([FromBody] EInvoiceConfig request)
        {
            try
            {
                var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
                if (config == null)
                {
                    config = new EInvoiceConfig();
                    _context.EInvoiceConfigs.Add(config);
                }

                // Cáº­p nháº­t cÃ¡c trÆ°á»ng
                config.IsEnabled = request.IsEnabled;
                config.Provider = request.Provider;
                config.ApiUrl = request.ApiUrl;
                config.Username = request.Username;
                config.Password = request.Password;
                config.Token = request.Token;
                config.CompanyCode = request.CompanyCode;
                config.DefaultTemplate = request.DefaultTemplate;
                config.DefaultSymbol = request.DefaultSymbol;
                config.AutoIssue = request.AutoIssue;
                config.AutoSendEmail = request.AutoSendEmail;
                config.AutoSendSMS = request.AutoSendSMS;
                config.CompanyTaxCode = request.CompanyTaxCode;
                config.CompanyName = request.CompanyName;
                config.CompanyAddress = request.CompanyAddress;
                config.CompanyPhone = request.CompanyPhone;
                config.CompanyEmail = request.CompanyEmail;
                config.CompanyBankAccount = request.CompanyBankAccount;
                config.CompanyBankName = request.CompanyBankName;
                config.DefaultTaxRate = request.DefaultTaxRate;
                config.EmailTemplate = request.EmailTemplate;
                config.SMSTemplate = request.SMSTemplate;
                config.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(config);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lá»—i cáº­p nháº­t cáº¥u hÃ¬nh: {ex.Message}");
            }
        }

        #region Helper Methods

        private async Task<string> GenerateInvoiceNumber()
        {
            var today = DateTime.Today;
            var count = await _context.EInvoices
                .CountAsync(e => e.CreatedAt.Date == today);

            return $"HÄ{today:yyyyMMdd}{(count + 1):D4}";
        }

        private string GenerateAuthCode()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }

        private decimal GetTaxRateValue(string taxRate)
        {
            return taxRate switch
            {
                "0%" => 0,
                "5%" => 5,
                "10%" => 10,
                "KCT" => 0,
                "KKKNT" => 0,
                _ => 10
            };
        }

        #endregion

        #region Test Endpoints

        // POST: api/EInvoice/test-auth
        [HttpPost("test-auth")]
        public async Task<ActionResult> TestAuthentication([FromBody] TestAuthRequest request)
        {
            try
            {
                var tempConfig = new EInvoiceConfig
                {
                    ApiUrl = request.ApiUrl,
                    Username = request.Username,
                    Password = request.Password,
                    CompanyTaxCode = request.CompanyTaxCode,
                    IsEnabled = true
                };

                // Test authentication with VNPT
                var httpClient = new HttpClient();
                var logger = HttpContext.RequestServices.GetRequiredService<ILogger<VNPTEInvoiceService>>();
                var context = HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var testService = new VNPTEInvoiceService(httpClient, logger, context);
                
                // Try to get a simple status to test auth
                var testResponse = await testService.GetInvoiceStatusAsync("test", tempConfig);

                return Ok(new { success = testResponse.Success, message = testResponse.ErrorMessage ?? "Authentication successful" });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/test-create
        [HttpPost("test-create")]
        public async Task<ActionResult> TestCreateInvoice([FromBody] TestInvoiceRequest request)
        {
            try
            {
                // Get current config
                var config = await _context.EInvoiceConfigs.FirstOrDefaultAsync();
                if (config == null || !config.IsEnabled)
                {
                    return Ok(new { success = false, message = "ChÆ°a cáº¥u hÃ¬nh hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­" });
                }

                // Create test invoice
                var testInvoice = new EInvoice
                {
                    InvoiceNumber = $"TEST{DateTime.Now:yyyyMMddHHmmss}",
                    InvoiceTemplate = config.DefaultTemplate,
                    InvoiceSymbol = config.DefaultSymbol,
                    IssueDate = DateTime.Now,
                    SellerTaxCode = config.CompanyTaxCode ?? "",
                    SellerName = config.CompanyName ?? "",
                    SellerAddress = config.CompanyAddress,
                    SellerPhone = config.CompanyPhone,
                    SellerEmail = config.CompanyEmail,
                    BuyerName = request.BuyerName,
                    BuyerTaxCode = request.BuyerTaxCode,
                    BuyerAddress = request.BuyerAddress,
                    BuyerPhone = request.BuyerPhone,
                    BuyerEmail = request.BuyerEmail,
                    SubTotal = 100000,
                    TaxAmount = 10000,
                    TotalAmount = 110000,
                    Status = "draft",
                    Items = new List<EInvoiceItem>
                    {
                        new EInvoiceItem
                        {
                            LineNumber = 1,
                            ItemCode = "TEST001",
                            ItemName = "Sáº£n pháº©m test",
                            Unit = "CÃ¡i",
                            Quantity = 1,
                            UnitPrice = 100000,
                            LineTotal = 100000,
                            TaxRate = "10%",
                            TaxAmount = 10000,
                            TotalAmount = 110000
                        }
                    }
                };

                // Try to issue invoice
                var apiResponse = await _eInvoiceService.IssueInvoiceAsync(testInvoice, config);

                return Ok(new 
                { 
                    success = apiResponse.Success, 
                    message = apiResponse.Success ? "Táº¡o hÃ³a Ä‘Æ¡n test thÃ nh cÃ´ng" : apiResponse.ErrorMessage,
                    data = apiResponse.Success ? new 
                    {
                        transactionId = apiResponse.TransactionUuid,
                        authCode = apiResponse.InvoiceAuthCode,
                        viewLink = apiResponse.InvoiceUrl
                    } : null
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region PortalService APIs - Tra cá»©u, Download, Chuyá»ƒn Ä‘á»•i, BÃ¡o cÃ¡o

        // POST: api/EInvoice/portal/download-inv
        [HttpPost("portal/download-inv")]
        public async Task<ActionResult> DownloadInv([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.DownloadInv(request.InvToken!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/download-inv-no-pay
        [HttpPost("portal/download-inv-no-pay")]
        public async Task<ActionResult> DownloadInvNoPay([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.DownloadInvNoPay(request.InvToken!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/download-inv-fkey
        [HttpPost("portal/download-inv-fkey")]
        public async Task<ActionResult> DownloadInvFkey([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.DownloadInvFkey(request.Fkey!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/download-inv-pdf
        [HttpPost("portal/download-inv-pdf")]
        public async Task<ActionResult> DownloadInvPDF([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.DownloadInvPDF(request.InvToken!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/list-inv-from-no-to-no
        [HttpPost("portal/list-inv-from-no-to-no")]
        public async Task<ActionResult> ListInvFromNoToNo([FromBody] ListInvFromNoToNoRequest request)
        {
            try
            {
                var result = await _eInvoiceService.ListInvFromNoToNo(
                    request.InvFromNo, request.InvToNo, request.InvPattern, 
                    request.InvSerial, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/list-inv-by-cus
        [HttpPost("portal/list-inv-by-cus")]
        public async Task<ActionResult> ListInvByCus([FromBody] ListInvByCusRequest request)
        {
            try
            {
                var result = await _eInvoiceService.ListInvByCus(
                    request.CusCode, request.FromDate, request.ToDate, 
                    request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/get-inv-view
        [HttpPost("portal/get-inv-view")]
        public async Task<ActionResult> GetInvView([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.GetInvView(request.InvToken!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/get-inv-view-fkey
        [HttpPost("portal/get-inv-view-fkey")]
        public async Task<ActionResult> GetInvViewFkey([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.GetInvViewFkey(request.Fkey!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/convert-for-verify
        [HttpPost("portal/convert-for-verify")]
        public async Task<ActionResult> ConvertForVerify([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.ConvertForVerify(request.InvToken!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/convert-for-store
        [HttpPost("portal/convert-for-store")]
        public async Task<ActionResult> ConvertForStore([FromBody] PortalApiRequest request)
        {
            try
            {
                var result = await _eInvoiceService.ConvertForStore(request.InvToken!, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/get-cus
        [HttpPost("portal/get-cus")]
        public async Task<ActionResult> GetCus([FromBody] GetCusRequest request)
        {
            try
            {
                var result = await _eInvoiceService.GetCus(request.CusCode, request.Username, request.Password);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/get-inv-view-by-date
        [HttpPost("portal/get-inv-view-by-date")]
        public async Task<ActionResult> GetInvViewByDate([FromBody] GetInvViewByDateRequest request)
        {
            try
            {
                var result = await _eInvoiceService.GetInvViewByDate(
                    request.Username, request.Password, request.Pattern, 
                    request.Serial, request.FromDate, request.ToDate);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/download-inv-zip-fkey
        [HttpPost("portal/download-inv-zip-fkey")]
        public async Task<ActionResult> DownloadInvZipFkey([FromBody] DownloadZipRequest request)
        {
            try
            {
                var result = await _eInvoiceService.DownloadInvZipFkey(
                    request.Fkey!, request.Username, request.Password, request.CheckPayment);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/portal/download-inv-zip-token
        [HttpPost("portal/download-inv-zip-token")]
        public async Task<ActionResult> DownloadInvZipToken([FromBody] DownloadZipRequest request)
        {
            try
            {
                var result = await _eInvoiceService.DownloadInvZipToken(
                    request.InvToken!, request.Username, request.Password, request.CheckPayment);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region MTT (MÃ¡y tÃ­nh tiá»n) APIs

        // POST: api/EInvoice/mtt/import-and-publish
        [HttpPost("mtt/import-and-publish")]
        public async Task<ActionResult> ImportAndPublishInvMTT([FromBody] MTTImportRequest request)
        {
            try
            {
                var result = await _eInvoiceService.ImportAndPublishInvMTT(
                    request.Account, request.ACpass, request.XmlInvData, 
                    request.Username, request.Password, request.Pattern, 
                    request.Serial, request.Convert);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/mtt/import-by-pattern
        [HttpPost("mtt/import-by-pattern")]
        public async Task<ActionResult> ImportInvByPatternMTT([FromBody] MTTImportRequest request)
        {
            try
            {
                var result = await _eInvoiceService.ImportInvByPatternMTT(
                    request.Account, request.ACpass, request.XmlInvData, 
                    request.Username, request.Password, request.Pattern, 
                    request.Serial, request.Convert);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/EInvoice/mtt/send
        [HttpPost("mtt/send")]
        public async Task<ActionResult> SendInvMTT([FromBody] MTTSendRequest request)
        {
            try
            {
                var result = await _eInvoiceService.SendInvMTT(
                    request.Account, request.ACpass, request.Username, 
                    request.Password, request.Pattern, request.Serial, request.Fkey);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        #endregion

        // DEBUG: Check order items
        [HttpGet("debug/order/{orderId}")]
        public async Task<ActionResult> DebugOrder(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return NotFound($"Order {orderId} not found");
            }

            var result = new
            {
                orderId = order.OrderId,
                orderNumber = order.OrderNumber,
                totalAmount = order.TotalAmount,
                itemsCount = order.Items?.Count ?? 0,
                items = order.Items?.Select(item => new
                {
                    orderItemId = item.OrderItemId,
                    productId = item.ProductId,
                    productName = item.ProductName,
                    quantity = item.Quantity,
                    price = item.Price,
                    totalPrice = item.TotalPrice,
                    product = item.Product != null ? new
                    {
                        name = item.Product.Name,
                        barcode = item.Product.Barcode,
                        unit = item.Product.Unit
                    } : null
                }).ToList()
            };

            return Ok(result);
        }
    }

    #region Request Models

    public class CreateEInvoiceFromOrderRequest
    {
        public int OrderId { get; set; }
        public string? BuyerTaxCode { get; set; }
        public string? BuyerName { get; set; }
        public string? BuyerAddress { get; set; }
        public string? BuyerPhone { get; set; }
        public string? BuyerEmail { get; set; }
        public string? Notes { get; set; }
    }

    public class CancelEInvoiceRequest
    {
        public string Reason { get; set; } = "";
    }

    public class TestAuthRequest
    {
        public string ApiUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CompanyTaxCode { get; set; } = "";
    }

    public class TestInvoiceRequest
    {
        public string BuyerName { get; set; } = "";
        public string? BuyerTaxCode { get; set; }
        public string? BuyerAddress { get; set; }
        public string? BuyerPhone { get; set; }
        public string? BuyerEmail { get; set; }
    }

    public class PortalApiRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? InvToken { get; set; }
        public string? Fkey { get; set; }
    }

    public class ListInvFromNoToNoRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string InvFromNo { get; set; } = "";
        public string InvToNo { get; set; } = "";
        public string InvPattern { get; set; } = "";
        public string InvSerial { get; set; } = "";
    }

    public class ListInvByCusRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CusCode { get; set; } = "";
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";
    }

    public class GetCusRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string CusCode { get; set; } = "";
    }

    public class GetInvViewByDateRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Pattern { get; set; } = "";
        public string Serial { get; set; } = "";
        public string FromDate { get; set; } = "";
        public string ToDate { get; set; } = "";
    }

    public class DownloadZipRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string? InvToken { get; set; }
        public string? Fkey { get; set; }
        public bool CheckPayment { get; set; } = true;
    }

    public class MTTImportRequest
    {
        public string Account { get; set; } = "";
        public string ACpass { get; set; } = "";
        public string XmlInvData { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Pattern { get; set; } = "";
        public string Serial { get; set; } = "";
        public int Convert { get; set; } = 0;
    }

    public class MTTSendRequest
    {
        public string Account { get; set; } = "";
        public string ACpass { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string Pattern { get; set; } = "";
        public string Serial { get; set; } = "";
        public string Fkey { get; set; } = "";
    }

    #endregion
}
