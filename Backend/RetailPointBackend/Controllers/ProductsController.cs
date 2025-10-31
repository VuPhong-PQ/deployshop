using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IImageSearchService _imageSearchService;
        
        public ProductsController(AppDbContext context, IImageSearchService imageSearchService)
        {
            _context = context;
            _imageSearchService = imageSearchService;
        }


        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            try
            {
                // Log thông tin nhận được
                Console.WriteLine($"[CreateProduct] Nhận request: {System.Text.Json.JsonSerializer.Serialize(request)}");
                
                // Validate required fields
                if (string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { message = "Tên sản phẩm là bắt buộc" });
                }

                if (request.Price <= 0)
                {
                    return BadRequest(new { message = "Giá bán phải lớn hơn 0" });
                }

                if (!request.ProductGroupId.HasValue)
                {
                    return BadRequest(new { message = "Nhóm sản phẩm là bắt buộc" });
                }

                // Check if ProductGroup exists
                var productGroup = await _context.ProductGroups.FindAsync(request.ProductGroupId.Value);
                if (productGroup == null)
                {
                    return BadRequest(new { message = "Nhóm sản phẩm không tồn tại" });
                }

                // Create Product entity
                var product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Barcode = request.Barcode,
                    Price = request.Price,
                    CostPrice = request.CostPrice ?? 0,
                    ProductGroupId = request.ProductGroupId.Value,
                    StockQuantity = request.StockQuantity,
                    MinStockLevel = request.MinStockLevel,
                    Unit = request.Unit ?? "chiếc",
                    ImageUrl = request.ImageUrl,
                    IsFeatured = request.IsFeatured,
                    StoreId = null // Set to null for now, will handle multi-store later
                };
                
                // Tạo sản phẩm
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[CreateProduct] Đã lưu productId: {product.ProductId}");
                
                // Tự động tạo inventory transaction nếu có số lượng ban đầu
                if (product.StockQuantity > 0)
                {
                    var inventoryTransaction = new InventoryTransaction
                    {
                        ProductId = product.ProductId,
                        StaffId = 1, // TODO: Get from authentication context
                        Type = TransactionType.IN,
                        Quantity = product.StockQuantity,
                        UnitPrice = product.CostPrice ?? 0,
                        TotalValue = product.StockQuantity * (product.CostPrice ?? 0),
                        Reason = "Nhập kho ban đầu",
                        Notes = product.Description ?? "Tạo sản phẩm mới",
                        TransactionDate = DateTime.Now,
                        StockBefore = 0,
                        StockAfter = product.StockQuantity
                    };
                    
                    _context.InventoryTransactions.Add(inventoryTransaction);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"[CreateProduct] Đã tạo inventory transaction cho sản phẩm mới: {inventoryTransaction.TransactionId}");
                }
                
                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CreateProduct][ERROR] {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
            }
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult> GetProducts([FromQuery] int? storeId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var query = _context.Products.AsQueryable();
            
            // Filter by store if storeId is provided
            // Include products with null StoreId (shared products) and products belonging to the specific store
            if (storeId.HasValue)
            {
                query = query.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
            }
            
            // Get total count for pagination
            var totalCount = await query.CountAsync();
            
            // Apply pagination
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            return Ok(new 
            {
                Products = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET: api/products/low-stock
        [HttpGet("low-stock")]
        public async Task<ActionResult> GetLowStockProducts()
        {
            var products = await _context.Products
                .Where(p => p.StockQuantity <= p.MinStockLevel)
                .OrderBy(p => p.StockQuantity)
                .ToListAsync();

            var lowStockProducts = products.Select(p => new {
                p.ProductId,
                p.Name,
                p.Barcode,
                p.StockQuantity,
                p.MinStockLevel,
                p.Price,
                p.Unit,
                p.StockDeficit,
                p.IsOutOfStock,
                p.StockStatus
            }).ToList();

            return Ok(new 
            { 
                Count = lowStockProducts.Count,
                Products = lowStockProducts 
            });
        }

        // GET: api/products/featured
        [HttpGet("featured")]
        public async Task<ActionResult> GetFeaturedProducts([FromQuery] int? storeId = null)
        {
            var query = _context.Products.AsQueryable();
            
            // Filter by store if storeId is provided
            if (storeId.HasValue)
            {
                query = query.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
            }
            
            var featuredProducts = await query
                .Where(p => p.IsFeatured)
                .OrderBy(p => p.Name)
                .Take(20) // Tối đa 20 sản phẩm hay bán
                .Select(p => new {
                    p.ProductId,
                    p.Name,
                    p.Barcode,
                    p.Price,
                    p.StockQuantity,
                    p.Unit,
                    p.ImageUrl,
                    p.Description,
                    p.IsFeatured
                })
                .ToListAsync();

            return Ok(new 
            { 
                Count = featuredProducts.Count,
                Products = featuredProducts 
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return product;
        }
        // PUT: api/products/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] Product updatedProduct)
        {
            try
            {
                Console.WriteLine($"[UpdateProduct] Updating product ID: {id}");
                Console.WriteLine($"[UpdateProduct] Product data: {System.Text.Json.JsonSerializer.Serialize(updatedProduct)}");
                
                if (id != updatedProduct.ProductId)
                {
                    Console.WriteLine($"[UpdateProduct] ID mismatch: {id} != {updatedProduct.ProductId}");
                    return BadRequest(new { message = "ID không khớp" });
                }

                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    Console.WriteLine($"[UpdateProduct] Product not found: {id}");
                    return NotFound(new { message = "Sản phẩm không tồn tại" });
                }

                // Update properties (chỉ cập nhật khi có giá trị mới)
                existingProduct.Name = updatedProduct.Name;
                existingProduct.Barcode = updatedProduct.Barcode;
                existingProduct.CategoryId = updatedProduct.CategoryId;
                existingProduct.ProductGroupId = updatedProduct.ProductGroupId;
                existingProduct.Price = updatedProduct.Price;
                existingProduct.CostPrice = updatedProduct.CostPrice;
                existingProduct.StockQuantity = updatedProduct.StockQuantity;
                existingProduct.MinStockLevel = updatedProduct.MinStockLevel;
                existingProduct.Unit = updatedProduct.Unit;
                
                // Chỉ cập nhật ImageUrl khi có giá trị mới và không rỗng
                if (!string.IsNullOrEmpty(updatedProduct.ImageUrl))
                {
                    existingProduct.ImageUrl = updatedProduct.ImageUrl;
                }
                
                existingProduct.Description = updatedProduct.Description;
                existingProduct.IsFeatured = updatedProduct.IsFeatured;

                await _context.SaveChangesAsync();
                Console.WriteLine($"[UpdateProduct] Successfully updated product: {id}");
                
                return Ok(new { message = "Cập nhật sản phẩm thành công", product = existingProduct });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"[UpdateProduct] Concurrency error: {ex.Message}");
                if (!_context.Products.Any(e => e.ProductId == id))
                {
                    return NotFound(new { message = "Sản phẩm không tồn tại" });
                }
                else
                {
                    return StatusCode(500, new { message = "Lỗi đồng thời", error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateProduct] Error: {ex.Message}");
                Console.WriteLine($"[UpdateProduct] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Không thể cập nhật sản phẩm", error = ex.Message });
            }
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/products/{id}/adjust-stock
        [HttpPost("{id}/adjust-stock")]
        public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustmentRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại");
            }

            var oldStock = product.StockQuantity;
            product.StockQuantity = request.NewQuantity;

            try
            {
                await _context.SaveChangesAsync();
                
                var result = new
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    OldStock = oldStock,
                    NewStock = product.StockQuantity,
                    MinStockLevel = product.MinStockLevel,
                    IsLowStock = product.IsLowStock,
                    IsOutOfStock = product.IsOutOfStock,
                    StockStatus = product.StockStatus,
                    StockDeficit = product.StockDeficit,
                    Reason = request.Reason ?? "Điều chỉnh tồn kho"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật tồn kho", error = ex.Message });
            }
        }

        // GET: api/products/{id}/stock-info
        [HttpGet("{id}/stock-info")]
        public async Task<ActionResult> GetProductStockInfo(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Sản phẩm không tồn tại");
            }

            var stockInfo = new
            {
                product.ProductId,
                product.Name,
                product.StockQuantity,
                product.MinStockLevel,
                product.IsLowStock,
                product.IsOutOfStock,
                product.StockDeficit,
                product.StockStatus
            };

            return Ok(stockInfo);
        }

        // POST: api/products/{id}/toggle-featured
        [HttpPost("{id}/toggle-featured")]
        public async Task<ActionResult> ToggleFeatured(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Sản phẩm không tồn tại" });
                }

                product.IsFeatured = !product.IsFeatured;
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = product.IsFeatured ? "Đã thêm vào sản phẩm hay bán" : "Đã xóa khỏi sản phẩm hay bán",
                    productId = product.ProductId,
                    productName = product.Name,
                    isFeatured = product.IsFeatured
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật trạng thái sản phẩm", error = ex.Message });
            }
        }

        // GET: api/products/export-template
        [HttpGet("export-template")]
        public async Task<IActionResult> ExportTemplate()
        {
            try
            {
                // Set license cho EPPlus 5.x
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Products Template");

                // Thiết lập headers
                var headers = new[] { 
                    "Tên sản phẩm (*)", 
                    "Mã vạch", 
                    "Giá bán (*)", 
                    "Giá vốn", 
                    "Số lượng tồn kho (*)", 
                    "Mức tồn kho tối thiểu", 
                    "Đơn vị", 
                    "Mô tả",
                    "ID Nhóm sản phẩm",
                    "Tên nhóm sản phẩm",
                    "Sản phẩm hay bán (0/1)"
                };

                // Thêm headers vào hàng đầu tiên
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Lấy dữ liệu thực tế từ database
                var products = await _context.Products.ToListAsync(); // Lấy tất cả sản phẩm
                var productGroups = await _context.ProductGroups.ToListAsync();
                var groupDict = productGroups.ToDictionary(g => g.ProductGroupId, g => g.Name);

                // Thêm dữ liệu sản phẩm thực tế làm ví dụ
                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value = product.Name ?? "Tên sản phẩm";
                    worksheet.Cells[row, 2].Value = product.Barcode ?? "";
                    worksheet.Cells[row, 3].Value = (double)product.Price;
                    worksheet.Cells[row, 4].Value = product.CostPrice.HasValue ? (double)product.CostPrice.Value : 0;
                    worksheet.Cells[row, 5].Value = product.StockQuantity;
                    worksheet.Cells[row, 6].Value = product.MinStockLevel;
                    worksheet.Cells[row, 7].Value = product.Unit ?? "chiếc";
                    worksheet.Cells[row, 8].Value = product.Description ?? "";
                    worksheet.Cells[row, 9].Value = product.ProductGroupId ?? 1;
                    worksheet.Cells[row, 10].Value = product.ProductGroupId.HasValue && groupDict.ContainsKey(product.ProductGroupId.Value) 
                        ? groupDict[product.ProductGroupId.Value] : "";
                    worksheet.Cells[row, 11].Value = product.IsFeatured ? 1 : 0;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Tạo sheet thứ 2 cho danh sách nhóm sản phẩm
                var groupSheet = package.Workbook.Worksheets.Add("Danh sách nhóm sản phẩm");
                groupSheet.Cells[1, 1].Value = "ID Nhóm";
                groupSheet.Cells[1, 2].Value = "Tên nhóm";
                groupSheet.Cells[1, 1].Style.Font.Bold = true;
                groupSheet.Cells[1, 2].Style.Font.Bold = true;
                groupSheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                groupSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                groupSheet.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                groupSheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                // Thêm danh sách nhóm sản phẩm
                int groupRow = 2;
                foreach (var group in productGroups)
                {
                    groupSheet.Cells[groupRow, 1].Value = group.ProductGroupId;
                    groupSheet.Cells[groupRow, 2].Value = group.Name;
                    groupRow++;
                }
                groupSheet.Cells.AutoFitColumns();

                // Quay lại sheet đầu tiên và thêm ghi chú hướng dẫn
                worksheet.Select();
                int instructionRow = row + 1;
                worksheet.Cells[instructionRow, 1].Value = "HƯỚNG DẪN:";
                worksheet.Cells[instructionRow, 1].Style.Font.Bold = true;
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Các cột có dấu (*) là bắt buộc";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Giá bán và Giá vốn nhập bằng số (VNĐ)";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Số lượng tồn kho và Mức tồn kho tối thiểu nhập bằng số nguyên";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- ID Nhóm sản phẩm: xem sheet 'Danh sách nhóm sản phẩm'";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Tên nhóm sản phẩm: nếu nhập tên mới, hệ thống sẽ tự tạo nhóm mới";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Nếu ID nhóm không tồn tại + có tên nhóm: tạo nhóm mới";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Có thể để trống ID nhóm và chỉ nhập tên nhóm để tạo nhóm mới";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Sản phẩm hay bán: nhập 1 (hay bán) hoặc 0 (thường)";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Các dòng dữ liệu mẫu phía trên có thể sửa đổi hoặc xóa";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Thêm dòng mới phía dưới để nhập sản phẩm mới";

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Products_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo template Excel", error = ex.Message });
            }
        }

        // GET: api/products/export
        [HttpGet("export")]
        public async Task<IActionResult> ExportProducts()
        {
            try
            {
                // Set license cho EPPlus 5.x
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Danh sách sản phẩm");

                // Thiết lập headers
                var headers = new[] {
                    "ID Nhóm sản phẩm",
                    "Tên nhóm sản phẩm",
                    "Mã vạch",
                    "Tên sản phẩm",
                    "Giá bán",
                    "Giá vốn",
                    "Số lượng tồn kho",
                    "Mức tồn kho tối thiểu",
                    "Đơn vị",
                    "Mô tả",
                    "Sản phẩm hay bán (0/1)"
                };

                // Thêm headers vào hàng đầu tiên
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Lấy tất cả sản phẩm từ database
                var products = await _context.Products
                    .OrderBy(p => p.ProductId)
                    .ToListAsync();

                // Lấy danh sách nhóm sản phẩm để mapping tên
                var productGroups = await _context.ProductGroups.ToListAsync();
                var groupDict = productGroups.ToDictionary(g => g.ProductGroupId, g => g.Name);

                // Thêm dữ liệu sản phẩm
                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value = product.ProductGroupId;
                    worksheet.Cells[row, 2].Value = product.ProductGroupId.HasValue && groupDict.ContainsKey(product.ProductGroupId.Value) 
                        ? groupDict[product.ProductGroupId.Value] : "";
                    worksheet.Cells[row, 3].Value = product.Barcode ?? "";
                    worksheet.Cells[row, 4].Value = product.Name;
                    worksheet.Cells[row, 5].Value = product.Price;
                    worksheet.Cells[row, 6].Value = product.CostPrice;
                    worksheet.Cells[row, 7].Value = product.StockQuantity;
                    worksheet.Cells[row, 8].Value = product.MinStockLevel;
                    worksheet.Cells[row, 9].Value = product.Unit ?? "chiếc";
                    worksheet.Cells[row, 10].Value = product.Description ?? "";
                    worksheet.Cells[row, 11].Value = product.IsFeatured ? 1 : 0;

                    // Định dạng cho các ô
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    row++;
                }

                // Tự động điều chỉnh độ rộng cột
                worksheet.Cells.AutoFitColumns();

                // Thiết lập chiều rộng tối thiểu cho các cột
                for (int col = 1; col <= headers.Length; col++)
                {
                    if (worksheet.Column(col).Width < 10)
                        worksheet.Column(col).Width = 10;
                    if (worksheet.Column(col).Width > 50)
                        worksheet.Column(col).Width = 50;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Products_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting products: {ex.Message}");
                return StatusCode(500, new { message = "Lỗi khi xuất dữ liệu sản phẩm", error = ex.Message });
            }
        }

        // POST: api/products/import-excel
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file Excel để upload" });
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new { message = "Chỉ hỗ trợ file Excel (.xlsx, .xls)" });
            }

            try
            {
                var importResults = new List<object>();
                var errors = new List<string>();
                var successCount = 0;
                var skippedCount = 0;

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                
                if (worksheet == null)
                {
                    return BadRequest(new { message = "File Excel không có worksheet nào" });
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount < 2)
                {
                    return BadRequest(new { message = "File Excel không có dữ liệu để import" });
                }

                // Lấy danh sách ProductGroup để validate
                var productGroups = await _context.ProductGroups.ToListAsync();
                var validGroupIds = productGroups.Select(pg => pg.ProductGroupId).ToHashSet();

                // Đọc từ hàng 2 (bỏ qua header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Đọc dữ liệu từ các cột
                        var name = worksheet.Cells[row, 1].Text?.Trim();
                        var barcode = worksheet.Cells[row, 2].Text?.Trim();
                        var priceText = worksheet.Cells[row, 3].Text?.Trim();
                        var costPriceText = worksheet.Cells[row, 4].Text?.Trim();
                        var stockText = worksheet.Cells[row, 5].Text?.Trim();
                        var minStockText = worksheet.Cells[row, 6].Text?.Trim();
                        var unit = worksheet.Cells[row, 7].Text?.Trim();
                        var description = worksheet.Cells[row, 8].Text?.Trim();
                        var productGroupIdText = worksheet.Cells[row, 9].Text?.Trim();
                        var productGroupName = worksheet.Cells[row, 10].Text?.Trim();
                        var isFeaturedText = worksheet.Cells[row, 11].Text?.Trim();

                        // Kiểm tra các trường bắt buộc
                        if (string.IsNullOrEmpty(name))
                        {
                            errors.Add($"Hàng {row}: Tên sản phẩm không được để trống");
                            continue;
                        }

                        if (string.IsNullOrEmpty(priceText) || !decimal.TryParse(priceText, out var price) || price < 0)
                        {
                            errors.Add($"Hàng {row}: Giá bán không hợp lệ");
                            continue;
                        }

                        if (string.IsNullOrEmpty(stockText) || !int.TryParse(stockText, out var stock) || stock < 0)
                        {
                            errors.Add($"Hàng {row}: Số lượng tồn kho không hợp lệ");
                            continue;
                        }

                        // Xử lý ProductGroup - tự động tạo mới nếu cần
                        int? finalProductGroupId = null;
                        
                        // Trường hợp 1: Có ID nhóm sản phẩm
                        if (!string.IsNullOrEmpty(productGroupIdText) && int.TryParse(productGroupIdText, out var productGroupId))
                        {
                            if (validGroupIds.Contains(productGroupId))
                            {
                                // ID tồn tại - sử dụng ID này
                                finalProductGroupId = productGroupId;
                            }
                            else if (!string.IsNullOrEmpty(productGroupName))
                            {
                                // ID không tồn tại nhưng có tên nhóm - tạo nhóm mới với tên được cung cấp
                                var existingGroup = productGroups.FirstOrDefault(g => g.Name.ToLower() == productGroupName.ToLower());
                                if (existingGroup != null)
                                {
                                    finalProductGroupId = existingGroup.ProductGroupId;
                                }
                                else
                                {
                                    // Tạo nhóm sản phẩm mới
                                    var newGroup = new ProductGroup { Name = productGroupName };
                                    _context.ProductGroups.Add(newGroup);
                                    await _context.SaveChangesAsync();
                                    
                                    finalProductGroupId = newGroup.ProductGroupId;
                                    productGroups.Add(newGroup); // Thêm vào danh sách để tránh tạo trùng
                                    validGroupIds.Add(newGroup.ProductGroupId);
                                }
                            }
                            else
                            {
                                // ID không tồn tại và không có tên nhóm
                                errors.Add($"Hàng {row}: ID Nhóm sản phẩm {productGroupId} không tồn tại. Vui lòng cung cấp tên nhóm để tạo mới.");
                                continue;
                            }
                        }
                        // Trường hợp 2: Chỉ có tên nhóm - tìm hoặc tạo mới
                        else if (!string.IsNullOrEmpty(productGroupName))
                        {
                            var existingGroup = productGroups.FirstOrDefault(g => g.Name.ToLower() == productGroupName.ToLower());
                            if (existingGroup != null)
                            {
                                finalProductGroupId = existingGroup.ProductGroupId;
                            }
                            else
                            {
                                // Tạo nhóm sản phẩm mới
                                var newGroup = new ProductGroup { Name = productGroupName };
                                _context.ProductGroups.Add(newGroup);
                                await _context.SaveChangesAsync();
                                
                                finalProductGroupId = newGroup.ProductGroupId;
                                productGroups.Add(newGroup); // Thêm vào danh sách để tránh tạo trùng
                                validGroupIds.Add(newGroup.ProductGroupId);
                            }
                        }
                        // Trường hợp 3: Không có cả ID và tên
                        else
                        {
                            errors.Add($"Hàng {row}: Phải có ít nhất ID nhóm sản phẩm hoặc tên nhóm sản phẩm");
                            continue;
                        }

                        // Parse các trường không bắt buộc
                        decimal.TryParse(costPriceText, out var costPrice);
                        int.TryParse(minStockText, out var minStock);
                        if (minStock <= 0) minStock = 5; // Giá trị mặc định
                        
                        // Parse IsFeatured (1 = hay bán, 0 hoặc empty = thường)
                        bool isFeatured = false;
                        if (!string.IsNullOrEmpty(isFeaturedText) && int.TryParse(isFeaturedText, out var featuredValue))
                        {
                            isFeatured = featuredValue == 1;
                        }

                        // Kiểm tra trùng tên sản phẩm
                        var existingProductByName = await _context.Products.FirstOrDefaultAsync(p => !string.IsNullOrEmpty(p.Name) && p.Name.ToLower() == name.ToLower());
                        if (existingProductByName != null)
                        {
                            skippedCount++;
                            importResults.Add(new
                            {
                                Row = row,
                                Name = name,
                                Status = "Skipped",
                                Reason = $"Sản phẩm đã tồn tại (ID: {existingProductByName.ProductId})"
                            });
                            continue;
                        }

                        // Kiểm tra trùng mã vạch nếu có
                        if (!string.IsNullOrEmpty(barcode))
                        {
                            var existingProduct = await _context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode);
                            if (existingProduct != null)
                            {
                                skippedCount++;
                                importResults.Add(new
                                {
                                    Row = row,
                                    Name = name,
                                    Status = "Skipped",
                                    Reason = $"Mã vạch đã tồn tại"
                                });
                                continue;
                            }
                        }

                        // Tạo sản phẩm mới
                        var product = new Product
                        {
                            Name = name,
                            Barcode = string.IsNullOrEmpty(barcode) ? null : barcode,
                            Price = price,
                            CostPrice = costPrice > 0 ? costPrice : null,
                            StockQuantity = stock,
                            MinStockLevel = minStock,
                            Unit = string.IsNullOrEmpty(unit) ? "chiếc" : unit,
                            Description = string.IsNullOrEmpty(description) ? null : description,
                            ProductGroupId = finalProductGroupId,
                            IsFeatured = isFeatured
                        };

                        _context.Products.Add(product);
                        await _context.SaveChangesAsync();

                        successCount++;
                        importResults.Add(new
                        {
                            Row = row,
                            ProductId = product.ProductId,
                            Name = product.Name,
                            Status = "Success"
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Hàng {row}: Lỗi khi tạo sản phẩm - {ex.Message}");
                    }
                }

                return Ok(new
                {
                    Message = $"Import hoàn tất. Thành công: {successCount}, Bỏ qua: {skippedCount}, Lỗi: {errors.Count}",
                    SuccessCount = successCount,
                    SkippedCount = skippedCount,
                    ErrorCount = errors.Count,
                    TotalProcessed = successCount + skippedCount + errors.Count,
                    Errors = errors,
                    Results = importResults
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi xử lý file Excel", error = ex.Message });
            }
        }

        // AI Image Search Endpoints
        [HttpPost("search-image")]
        public async Task<IActionResult> SearchAndDownloadImage([FromBody] ImageSearchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProductName))
                {
                    return BadRequest(new { message = "Tên sản phẩm là bắt buộc" });
                }

                var imageUrl = await _imageSearchService.SearchAndDownloadImageAsync(
                request.ProductName, 
                request.ProductGroupName, 
                request.Description
            );
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return NotFound(new { message = "Không tìm thấy hình ảnh phù hợp" });
                }

                return Ok(new { imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tìm kiếm hình ảnh", error = ex.Message });
            }
        }

        [HttpPost("search-images")]
        public async Task<IActionResult> SearchImages([FromBody] ImageSearchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProductName))
                {
                    return BadRequest(new { message = "Tên sản phẩm là bắt buộc" });
                }

            var limit = request.Limit ?? 5;
            var imageUrls = await _imageSearchService.SearchImagesAsync(
                request.ProductName, 
                limit, 
                request.ProductGroupName, 
                request.Description
            );                return Ok(new { images = imageUrls });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tìm kiếm hình ảnh", error = ex.Message });
            }
        }
    }

    // Model cho request điều chỉnh tồn kho
    public class StockAdjustmentRequest
    {
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
    }

    // Model cho request tạo sản phẩm mới
    public class CreateProductRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int? ProductGroupId { get; set; }
        public int StockQuantity { get; set; }
        public int MinStockLevel { get; set; } = 5;
        public string? Unit { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsFeatured { get; set; } = false;
    }

    public class ImageSearchRequest
    {
        public string ProductName { get; set; } = string.Empty;
        public string? ProductGroupName { get; set; }
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public int? Limit { get; set; }
    }
}
