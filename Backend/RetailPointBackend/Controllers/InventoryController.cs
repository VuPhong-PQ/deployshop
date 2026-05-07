using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using OfficeOpenXml;
using System.ComponentModel.DataAnnotations;

namespace RetailPointBackend.Controllers
{
    public class ImportResult
    {
        public int Row { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int? OldStock { get; set; }
        public int? NewStock { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InventoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Inventory/transactions
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<InventoryTransactionResponseDto>>> GetTransactions(
            [FromQuery] int? productId = null,
            [FromQuery] TransactionType? type = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                Console.WriteLine($"Getting inventory transactions - ProductId: {productId}, Type: {type}, Page: {page}");

                var query = _context.InventoryTransactions
                    .Include(t => t.Product)
                    .Include(t => t.Staff)
                    .Include(t => t.Order)
                    .AsQueryable();

                // Apply filters
                if (productId.HasValue)
                    query = query.Where(t => t.ProductId == productId.Value);

                if (type.HasValue)
                    query = query.Where(t => t.Type == type.Value);

                if (fromDate.HasValue)
                    query = query.Where(t => t.TransactionDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(t => t.TransactionDate <= toDate.Value);

                // Apply pagination
                var totalCount = await query.CountAsync();
                var transactions = await query
                    .OrderByDescending(t => t.TransactionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new InventoryTransactionResponseDto
                    {
                        TransactionId = t.TransactionId,
                        ProductId = t.ProductId,
                        ProductName = t.Product.Name ?? "",
                        ProductCode = t.Product.Barcode ?? "",
                        StaffId = t.StaffId,
                        StaffName = t.Staff.FullName,
                        Type = t.Type == TransactionType.IN ? "IN" : "OUT",
                        TypeName = t.Type == TransactionType.IN ? "Nháº­p kho" : "Xuáº¥t kho",
                        Quantity = t.Quantity,
                        UnitPrice = t.UnitPrice,
                        TotalValue = t.TotalValue,
                        Reason = t.Reason,
                        Notes = t.Notes,
                        OrderId = t.OrderId,
                        SupplierId = t.SupplierId,
                        SupplierName = t.SupplierName,
                        ReferenceNumber = t.ReferenceNumber,
                        TransactionDate = t.TransactionDate,
                        CreatedAt = t.CreatedAt,
                        StockBefore = t.StockBefore,
                        StockAfter = t.StockAfter
                    })
                    .ToListAsync();

                Console.WriteLine($"Found {transactions.Count} transactions out of {totalCount} total");

                return Ok(new
                {
                    data = transactions,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting inventory transactions: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y lá»‹ch sá»­ xuáº¥t nháº­p kho", error = ex.Message });
            }
        }

        // GET: api/Inventory/summary
        [HttpGet("summary")]
        public async Task<ActionResult<IEnumerable<InventorySummaryDto>>> GetInventorySummary()
        {
            try
            {
                Console.WriteLine("Getting inventory summary...");

                var summary = await _context.Products
                    .Select(p => new InventorySummaryDto
                    {
                        ProductId = p.ProductId,
                        ProductName = p.Name ?? "",
                        ProductCode = p.Barcode ?? "",
                        CurrentStock = p.StockQuantity,
                        TotalInbound = _context.InventoryTransactions
                            .Where(t => t.ProductId == p.ProductId && t.Type == TransactionType.IN)
                            .Sum(t => (int?)t.Quantity) ?? 0,
                        TotalOutbound = _context.InventoryTransactions
                            .Where(t => t.ProductId == p.ProductId && t.Type == TransactionType.OUT)
                            .Sum(t => (int?)Math.Abs(t.Quantity)) ?? 0,
                        TotalInboundValue = _context.InventoryTransactions
                            .Where(t => t.ProductId == p.ProductId && t.Type == TransactionType.IN)
                            .Sum(t => (decimal?)t.TotalValue) ?? 0,
                        TotalOutboundValue = _context.InventoryTransactions
                            .Where(t => t.ProductId == p.ProductId && t.Type == TransactionType.OUT)
                            .Sum(t => (decimal?)Math.Abs(t.TotalValue)) ?? 0,
                        LastTransaction = _context.InventoryTransactions
                            .Where(t => t.ProductId == p.ProductId)
                            .OrderByDescending(t => t.TransactionDate)
                            .Select(t => (DateTime?)t.TransactionDate)
                            .FirstOrDefault()
                    })
                    .ToListAsync();

                Console.WriteLine($"Generated summary for {summary.Count} products");
                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting inventory summary: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y tá»•ng há»£p kho", error = ex.Message });
            }
        }

        // POST: api/Inventory/inbound
        [HttpPost("inbound")]
        public async Task<ActionResult<InventoryTransactionResponseDto>> CreateInboundTransaction([FromBody] CreateInboundTransactionDto dto)
        {
            try
            {
                Console.WriteLine($"Creating inbound transaction for Product {dto.ProductId}, Quantity: {dto.Quantity}");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify product exists
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                // Get current user (for now, use a default staff ID - you should get this from authentication)
                var staffId = 1; // TODO: Get from authentication context

                var stockBefore = product.StockQuantity;
                var stockAfter = stockBefore + dto.Quantity;

                var transaction = new InventoryTransaction
                {
                    ProductId = dto.ProductId,
                    StaffId = staffId,
                    Type = TransactionType.IN,
                    Quantity = dto.Quantity,
                    UnitPrice = dto.UnitPrice,
                    TotalValue = dto.Quantity * dto.UnitPrice,
                    Reason = dto.Reason,
                    Notes = dto.Notes,
                    SupplierId = dto.SupplierId,
                    SupplierName = dto.SupplierName,
                    ReferenceNumber = dto.ReferenceNumber,
                    TransactionDate = dto.TransactionDate ?? DateTime.Now,
                    StockBefore = stockBefore,
                    StockAfter = stockAfter
                };

                // Update product stock
                product.StockQuantity = stockAfter;

                _context.InventoryTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Inbound transaction created with ID: {transaction.TransactionId}");

                // Return the created transaction
                var result = new InventoryTransactionResponseDto
                {
                    TransactionId = transaction.TransactionId,
                    ProductId = transaction.ProductId,
                    ProductName = product.Name ?? "",
                    ProductCode = product.Barcode ?? "",
                    StaffId = transaction.StaffId,
                    StaffName = "Admin", // TODO: Get from staff entity
                    Type = "IN",
                    TypeName = "Nháº­p kho",
                    Quantity = transaction.Quantity,
                    UnitPrice = transaction.UnitPrice,
                    TotalValue = transaction.TotalValue,
                    Reason = transaction.Reason,
                    Notes = transaction.Notes,
                    SupplierId = transaction.SupplierId,
                    SupplierName = transaction.SupplierName,
                    ReferenceNumber = transaction.ReferenceNumber,
                    TransactionDate = transaction.TransactionDate,
                    CreatedAt = transaction.CreatedAt,
                    StockBefore = transaction.StockBefore,
                    StockAfter = transaction.StockAfter
                };

                return CreatedAtAction(nameof(GetTransactions), new { id = transaction.TransactionId }, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating inbound transaction: {ex.Message}");
                return StatusCode(500, new { message = "KhÃ´ng thá»ƒ táº¡o giao dá»‹ch nháº­p kho", error = ex.Message });
            }
        }

        // POST: api/Inventory/outbound
        [HttpPost("outbound")]
        public async Task<ActionResult<InventoryTransactionResponseDto>> CreateOutboundTransaction([FromBody] CreateOutboundTransactionDto dto)
        {
            try
            {
                Console.WriteLine($"Creating outbound transaction for Product {dto.ProductId}, Quantity: {dto.Quantity}");

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Verify product exists and has enough stock
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                if (product.StockQuantity < dto.Quantity)
                {
                    return BadRequest(new { message = $"KhÃ´ng Ä‘á»§ hÃ ng tá»“n kho. Hiá»‡n cÃ³: {product.StockQuantity}, yÃªu cáº§u: {dto.Quantity}" });
                }

                // Get current user (for now, use a default staff ID)
                var staffId = 1; // TODO: Get from authentication context

                var stockBefore = product.StockQuantity;
                var stockAfter = stockBefore - dto.Quantity;

                var transaction = new InventoryTransaction
                {
                    ProductId = dto.ProductId,
                    StaffId = staffId,
                    Type = TransactionType.OUT,
                    Quantity = -dto.Quantity, // Negative for outbound
                    UnitPrice = product.Price, // Use current product price
                    TotalValue = -dto.Quantity * product.Price,
                    Reason = dto.Reason,
                    Notes = dto.Notes,
                    OrderId = dto.OrderId,
                    ReferenceNumber = dto.ReferenceNumber,
                    TransactionDate = dto.TransactionDate ?? DateTime.Now,
                    StockBefore = stockBefore,
                    StockAfter = stockAfter
                };

                // Update product stock
                product.StockQuantity = stockAfter;

                _context.InventoryTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Outbound transaction created with ID: {transaction.TransactionId}");

                // Return the created transaction
                var result = new InventoryTransactionResponseDto
                {
                    TransactionId = transaction.TransactionId,
                    ProductId = transaction.ProductId,
                    ProductName = product.Name ?? "",
                    ProductCode = product.Barcode ?? "",
                    StaffId = transaction.StaffId,
                    StaffName = "Admin", // TODO: Get from staff entity
                    Type = "OUT",
                    TypeName = "Xuáº¥t kho",
                    Quantity = transaction.Quantity,
                    UnitPrice = transaction.UnitPrice,
                    TotalValue = transaction.TotalValue,
                    Reason = transaction.Reason,
                    Notes = transaction.Notes,
                    OrderId = transaction.OrderId,
                    ReferenceNumber = transaction.ReferenceNumber,
                    TransactionDate = transaction.TransactionDate,
                    CreatedAt = transaction.CreatedAt,
                    StockBefore = transaction.StockBefore,
                    StockAfter = transaction.StockAfter
                };

                return CreatedAtAction(nameof(GetTransactions), new { id = transaction.TransactionId }, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating outbound transaction: {ex.Message}");
                return StatusCode(500, new { message = "KhÃ´ng thá»ƒ táº¡o giao dá»‹ch xuáº¥t kho", error = ex.Message });
            }
        }

        // GET: api/Inventory/transactions/{id}
        [HttpGet("transactions/{id}")]
        public async Task<ActionResult<InventoryTransactionResponseDto>> GetTransaction(int id)
        {
            try
            {
                var transaction = await _context.InventoryTransactions
                    .Include(t => t.Product)
                    .Include(t => t.Staff)
                    .Include(t => t.Order)
                    .Where(t => t.TransactionId == id)
                    .Select(t => new InventoryTransactionResponseDto
                    {
                        TransactionId = t.TransactionId,
                        ProductId = t.ProductId,
                        ProductName = t.Product.Name ?? "",
                        ProductCode = t.Product.Barcode ?? "",
                        StaffId = t.StaffId,
                        StaffName = t.Staff.FullName,
                        Type = t.Type == TransactionType.IN ? "IN" : "OUT",
                        TypeName = t.Type == TransactionType.IN ? "Nháº­p kho" : "Xuáº¥t kho",
                        Quantity = t.Quantity,
                        UnitPrice = t.UnitPrice,
                        TotalValue = t.TotalValue,
                        Reason = t.Reason,
                        Notes = t.Notes,
                        OrderId = t.OrderId,
                        SupplierId = t.SupplierId,
                        SupplierName = t.SupplierName,
                        ReferenceNumber = t.ReferenceNumber,
                        TransactionDate = t.TransactionDate,
                        CreatedAt = t.CreatedAt,
                        StockBefore = t.StockBefore,
                        StockAfter = t.StockAfter
                    })
                    .FirstOrDefaultAsync();

                if (transaction == null)
                {
                    return NotFound(new { message = "Giao dá»‹ch khÃ´ng tá»“n táº¡i" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting transaction {id}: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y thÃ´ng tin giao dá»‹ch", error = ex.Message });
            }
        }

        // GET: api/Inventory/export-template
        [HttpGet("export-template")]
        public async Task<IActionResult> ExportTemplate()
        {
            try
            {
                var products = await _context.Products
                    .Select(p => new
                    {
                        p.ProductId,
                        p.Name,
                        SKU = p.Barcode, // Use Barcode as SKU
                        p.StockQuantity,
                        p.MinStockLevel,
                        p.Price
                    })
                    .ToListAsync();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                
                // Táº¡o worksheet cho template
                var worksheet = package.Workbook.Worksheets.Add("Template Tá»“n Kho");
                
                // ThÃªm header
                worksheet.Cells[1, 1].Value = "ID Sáº£n Pháº©m";
                worksheet.Cells[1, 2].Value = "TÃªn Sáº£n Pháº©m";
                worksheet.Cells[1, 3].Value = "SKU";
                worksheet.Cells[1, 4].Value = "Tá»“n Kho Hiá»‡n Táº¡i";
                worksheet.Cells[1, 5].Value = "Tá»“n Kho Má»›i";
                worksheet.Cells[1, 6].Value = "LÃ½ Do Thay Äá»•i";
                worksheet.Cells[1, 7].Value = "GiÃ¡";
                
                // Format header
                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                
                // ThÃªm dá»¯ liá»‡u sáº£n pháº©m
                for (int i = 0; i < products.Count; i++)
                {
                    int row = i + 2;
                    var product = products[i];
                    
                    worksheet.Cells[row, 1].Value = product.ProductId;
                    worksheet.Cells[row, 2].Value = product.Name;
                    worksheet.Cells[row, 3].Value = product.SKU ?? "";
                    worksheet.Cells[row, 4].Value = product.StockQuantity;
                    worksheet.Cells[row, 5].Value = ""; // Äá»ƒ trá»‘ng cho ngÆ°á»i dÃ¹ng nháº­p
                    worksheet.Cells[row, 6].Value = ""; // Äá»ƒ trá»‘ng cho ngÆ°á»i dÃ¹ng nháº­p lÃ½ do
                    worksheet.Cells[row, 7].Value = product.Price;
                }
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                // Táº¡o worksheet hÆ°á»›ng dáº«n
                var instructionWs = package.Workbook.Worksheets.Add("HÆ°á»›ng Dáº«n");
                instructionWs.Cells[1, 1].Value = "HÆ¯á»šNG DáºªN Sá»¬ Dá»¤NG TEMPLATE Tá»’NG KHO";
                instructionWs.Cells[1, 1].Style.Font.Bold = true;
                instructionWs.Cells[1, 1].Style.Font.Size = 14;
                
                instructionWs.Cells[3, 1].Value = "1. Chá»‰ Ä‘Æ°á»£c thay Ä‘á»•i cá»™t 'Tá»“n Kho Má»›i' vÃ  'LÃ½ Do Thay Äá»•i'";
                instructionWs.Cells[4, 1].Value = "2. KhÃ´ng Ä‘Æ°á»£c thay Ä‘á»•i ID Sáº£n Pháº©m, TÃªn, SKU, hoáº·c Tá»“n Kho Hiá»‡n Táº¡i";
                instructionWs.Cells[5, 1].Value = "3. LÃ½ do thay Ä‘á»•i lÃ  báº¯t buá»™c khi cáº­p nháº­t tá»“n kho";
                instructionWs.Cells[6, 1].Value = "4. Náº¿u trÃ¹ng ID hoáº·c tÃªn sáº£n pháº©m, há»‡ thá»‘ng sáº½ bá» qua";
                instructionWs.Cells[7, 1].Value = "5. Chá»‰ nháº­p sá»‘ nguyÃªn dÆ°Æ¡ng cho cá»™t 'Tá»“n Kho Má»›i'";
                
                instructionWs.Cells.AutoFitColumns();
                
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                
                var fileName = $"Template_TonKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting template: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi xuáº¥t template", error = ex.Message });
            }
        }

        // POST: api/Inventory/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportInventory(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lÃ²ng chá»n file Excel" });
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new { message = "Chá»‰ cháº¥p nháº­n file Excel (.xlsx, .xls)" });
            }

            try
            {
                var results = new List<ImportResult>();
                var transactions = new List<InventoryTransaction>();
                
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                {
                    return BadRequest(new { message = "File Excel khÃ´ng cÃ³ dá»¯ liá»‡u" });
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount < 2)
                {
                    return BadRequest(new { message = "File Excel khÃ´ng cÃ³ dá»¯ liá»‡u Ä‘á»ƒ import" });
                }

                // Láº¥y danh sÃ¡ch sáº£n pháº©m hiá»‡n cÃ³
                var existingProducts = await _context.Products.ToListAsync();
                var existingProductIds = existingProducts.Select(p => p.ProductId).ToHashSet();
                var existingProductNames = existingProducts
                    .Where(p => p.Name != null)
                    .ToDictionary(p => p.Name!.ToLower(), p => p);

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var productIdCell = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var productName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var newStockCell = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        var reason = worksheet.Cells[row, 6].Value?.ToString()?.Trim();

                        var result = new ImportResult
                        {
                            Row = row,
                            ProductName = productName ?? "",
                            Status = "ThÃ nh cÃ´ng"
                        };

                        // Validate dá»¯ liá»‡u
                        if (string.IsNullOrEmpty(productIdCell) && string.IsNullOrEmpty(productName))
                        {
                            result.Status = "Bá» qua - Thiáº¿u thÃ´ng tin sáº£n pháº©m";
                            results.Add(result);
                            continue;
                        }

                        if (string.IsNullOrEmpty(newStockCell))
                        {
                            result.Status = "Bá» qua - KhÃ´ng cÃ³ tá»“n kho má»›i";
                            results.Add(result);
                            continue;
                        }

                        if (string.IsNullOrEmpty(reason))
                        {
                            result.Status = "Lá»—i - Thiáº¿u lÃ½ do thay Ä‘á»•i";
                            results.Add(result);
                            continue;
                        }

                        if (!int.TryParse(newStockCell, out int newStock) || newStock < 0)
                        {
                            result.Status = "Lá»—i - Tá»“n kho má»›i khÃ´ng há»£p lá»‡";
                            results.Add(result);
                            continue;
                        }

                        // TÃ¬m sáº£n pháº©m
                        Product? product = null;
                        
                        if (int.TryParse(productIdCell, out int productId))
                        {
                            product = existingProducts.FirstOrDefault(p => p.ProductId == productId);
                        }
                        
                        if (product == null && !string.IsNullOrEmpty(productName))
                        {
                            existingProductNames.TryGetValue(productName.ToLower(), out product);
                        }

                        if (product == null)
                        {
                            result.Status = "Lá»—i - KhÃ´ng tÃ¬m tháº¥y sáº£n pháº©m";
                            results.Add(result);
                            continue;
                        }

                        // Kiá»ƒm tra trÃ¹ng láº·p trong batch hiá»‡n táº¡i
                        if (transactions.Any(t => t.ProductId == product.ProductId))
                        {
                            result.Status = "Bá» qua - Sáº£n pháº©m Ä‘Ã£ Ä‘Æ°á»£c cáº­p nháº­t trong batch nÃ y";
                            results.Add(result);
                            continue;
                        }

                        // Táº¡o transaction
                        var oldStock = product.StockQuantity;
                        var quantityChange = newStock - oldStock;
                        
                        if (quantityChange != 0)
                        {
                            var transaction = new InventoryTransaction
                            {
                                ProductId = product.ProductId,
                                Type = quantityChange > 0 ? TransactionType.IN : TransactionType.OUT,
                                Quantity = Math.Abs(quantityChange),
                                UnitPrice = product.Price,
                                TotalValue = Math.Abs(quantityChange) * product.Price,
                                StockBefore = oldStock,
                                StockAfter = newStock,
                                Notes = $"Import Excel: {reason}",
                                ReferenceNumber = $"IMP-{DateTime.Now:yyyyMMdd}-{row}",
                                TransactionDate = DateTime.Now,
                                StaffId = 1 // TODO: Get from current user session
                            };

                            transactions.Add(transaction);
                            product.StockQuantity = newStock; // Update for next iterations
                            
                            result.OldStock = oldStock;
                            result.NewStock = newStock;
                            result.ProductName = product.Name ?? "";
                        }
                        else
                        {
                            result.Status = "Bá» qua - KhÃ´ng cÃ³ thay Ä‘á»•i";
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ImportResult
                        {
                            Row = row,
                            Status = $"Lá»—i - {ex.Message}",
                            ProductName = worksheet.Cells[row, 2].Value?.ToString() ?? ""
                        });
                    }
                }

                // LÆ°u vÃ o database
                if (transactions.Any())
                {
                    _context.InventoryTransactions.AddRange(transactions);
                    await _context.SaveChangesAsync();
                }

                var summary = new
                {
                    TotalRows = rowCount - 1,
                    Successful = results.Count(r => r.Status == "ThÃ nh cÃ´ng"),
                    Skipped = results.Count(r => r.Status.StartsWith("Bá» qua")),
                    Errors = results.Count(r => r.Status.StartsWith("Lá»—i")),
                    Details = results
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing inventory: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi import dá»¯ liá»‡u", error = ex.Message });
            }
        }
    }
}
