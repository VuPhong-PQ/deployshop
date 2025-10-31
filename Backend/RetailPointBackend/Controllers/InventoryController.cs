using Microsoft.AspNetCore.Mvc;
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
                        TypeName = t.Type == TransactionType.IN ? "Nhập kho" : "Xuất kho",
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
                return StatusCode(500, new { message = "Lỗi khi lấy lịch sử xuất nhập kho", error = ex.Message });
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
                return StatusCode(500, new { message = "Lỗi khi lấy tổng hợp kho", error = ex.Message });
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
                    return NotFound(new { message = "Sản phẩm không tồn tại" });
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
                    TypeName = "Nhập kho",
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
                return StatusCode(500, new { message = "Không thể tạo giao dịch nhập kho", error = ex.Message });
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
                    return NotFound(new { message = "Sản phẩm không tồn tại" });
                }

                if (product.StockQuantity < dto.Quantity)
                {
                    return BadRequest(new { message = $"Không đủ hàng tồn kho. Hiện có: {product.StockQuantity}, yêu cầu: {dto.Quantity}" });
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
                    TypeName = "Xuất kho",
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
                return StatusCode(500, new { message = "Không thể tạo giao dịch xuất kho", error = ex.Message });
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
                        TypeName = t.Type == TransactionType.IN ? "Nhập kho" : "Xuất kho",
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
                    return NotFound(new { message = "Giao dịch không tồn tại" });
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting transaction {id}: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin giao dịch", error = ex.Message });
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
                
                // Tạo worksheet cho template
                var worksheet = package.Workbook.Worksheets.Add("Template Tồn Kho");
                
                // Thêm header
                worksheet.Cells[1, 1].Value = "ID Sản Phẩm";
                worksheet.Cells[1, 2].Value = "Tên Sản Phẩm";
                worksheet.Cells[1, 3].Value = "SKU";
                worksheet.Cells[1, 4].Value = "Tồn Kho Hiện Tại";
                worksheet.Cells[1, 5].Value = "Tồn Kho Mới";
                worksheet.Cells[1, 6].Value = "Lý Do Thay Đổi";
                worksheet.Cells[1, 7].Value = "Giá";
                
                // Format header
                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }
                
                // Thêm dữ liệu sản phẩm
                for (int i = 0; i < products.Count; i++)
                {
                    int row = i + 2;
                    var product = products[i];
                    
                    worksheet.Cells[row, 1].Value = product.ProductId;
                    worksheet.Cells[row, 2].Value = product.Name;
                    worksheet.Cells[row, 3].Value = product.SKU ?? "";
                    worksheet.Cells[row, 4].Value = product.StockQuantity;
                    worksheet.Cells[row, 5].Value = ""; // Để trống cho người dùng nhập
                    worksheet.Cells[row, 6].Value = ""; // Để trống cho người dùng nhập lý do
                    worksheet.Cells[row, 7].Value = product.Price;
                }
                
                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                // Tạo worksheet hướng dẫn
                var instructionWs = package.Workbook.Worksheets.Add("Hướng Dẫn");
                instructionWs.Cells[1, 1].Value = "HƯỚNG DẪN SỬ DỤNG TEMPLATE TỒNG KHO";
                instructionWs.Cells[1, 1].Style.Font.Bold = true;
                instructionWs.Cells[1, 1].Style.Font.Size = 14;
                
                instructionWs.Cells[3, 1].Value = "1. Chỉ được thay đổi cột 'Tồn Kho Mới' và 'Lý Do Thay Đổi'";
                instructionWs.Cells[4, 1].Value = "2. Không được thay đổi ID Sản Phẩm, Tên, SKU, hoặc Tồn Kho Hiện Tại";
                instructionWs.Cells[5, 1].Value = "3. Lý do thay đổi là bắt buộc khi cập nhật tồn kho";
                instructionWs.Cells[6, 1].Value = "4. Nếu trùng ID hoặc tên sản phẩm, hệ thống sẽ bỏ qua";
                instructionWs.Cells[7, 1].Value = "5. Chỉ nhập số nguyên dương cho cột 'Tồn Kho Mới'";
                
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
                return StatusCode(500, new { message = "Lỗi khi xuất template", error = ex.Message });
            }
        }

        // POST: api/Inventory/import
        [HttpPost("import")]
        public async Task<IActionResult> ImportInventory(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file Excel" });
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new { message = "Chỉ chấp nhận file Excel (.xlsx, .xls)" });
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
                    return BadRequest(new { message = "File Excel không có dữ liệu" });
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount < 2)
                {
                    return BadRequest(new { message = "File Excel không có dữ liệu để import" });
                }

                // Lấy danh sách sản phẩm hiện có
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
                            Status = "Thành công"
                        };

                        // Validate dữ liệu
                        if (string.IsNullOrEmpty(productIdCell) && string.IsNullOrEmpty(productName))
                        {
                            result.Status = "Bỏ qua - Thiếu thông tin sản phẩm";
                            results.Add(result);
                            continue;
                        }

                        if (string.IsNullOrEmpty(newStockCell))
                        {
                            result.Status = "Bỏ qua - Không có tồn kho mới";
                            results.Add(result);
                            continue;
                        }

                        if (string.IsNullOrEmpty(reason))
                        {
                            result.Status = "Lỗi - Thiếu lý do thay đổi";
                            results.Add(result);
                            continue;
                        }

                        if (!int.TryParse(newStockCell, out int newStock) || newStock < 0)
                        {
                            result.Status = "Lỗi - Tồn kho mới không hợp lệ";
                            results.Add(result);
                            continue;
                        }

                        // Tìm sản phẩm
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
                            result.Status = "Lỗi - Không tìm thấy sản phẩm";
                            results.Add(result);
                            continue;
                        }

                        // Kiểm tra trùng lặp trong batch hiện tại
                        if (transactions.Any(t => t.ProductId == product.ProductId))
                        {
                            result.Status = "Bỏ qua - Sản phẩm đã được cập nhật trong batch này";
                            results.Add(result);
                            continue;
                        }

                        // Tạo transaction
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
                            result.Status = "Bỏ qua - Không có thay đổi";
                        }

                        results.Add(result);
                    }
                    catch (Exception ex)
                    {
                        results.Add(new ImportResult
                        {
                            Row = row,
                            Status = $"Lỗi - {ex.Message}",
                            ProductName = worksheet.Cells[row, 2].Value?.ToString() ?? ""
                        });
                    }
                }

                // Lưu vào database
                if (transactions.Any())
                {
                    _context.InventoryTransactions.AddRange(transactions);
                    await _context.SaveChangesAsync();
                }

                var summary = new
                {
                    TotalRows = rowCount - 1,
                    Successful = results.Count(r => r.Status == "Thành công"),
                    Skipped = results.Count(r => r.Status.StartsWith("Bỏ qua")),
                    Errors = results.Count(r => r.Status.StartsWith("Lỗi")),
                    Details = results
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing inventory: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi import dữ liệu", error = ex.Message });
            }
        }
    }
}