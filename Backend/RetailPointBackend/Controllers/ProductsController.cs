using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace RetailPointBackend.Controllers
{
    [Authorize]
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
                // Log thÃ´ng tin nháº­n Ä‘Æ°á»£c
                Console.WriteLine($"[CreateProduct] Nháº­n request: {System.Text.Json.JsonSerializer.Serialize(request)}");
                
                // Validate required fields
                if (string.IsNullOrEmpty(request.Name))
                {
                    return BadRequest(new { message = "TÃªn sáº£n pháº©m lÃ  báº¯t buá»™c" });
                }

                if (request.Price <= 0)
                {
                    return BadRequest(new { message = "GiÃ¡ bÃ¡n pháº£i lá»›n hÆ¡n 0" });
                }

                if (!request.ProductGroupId.HasValue)
                {
                    return BadRequest(new { message = "NhÃ³m sáº£n pháº©m lÃ  báº¯t buá»™c" });
                }

                // Check if ProductGroup exists
                var productGroup = await _context.ProductGroups.FindAsync(request.ProductGroupId.Value);
                if (productGroup == null)
                {
                    return BadRequest(new { message = "NhÃ³m sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
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
                    Unit = request.Unit ?? "chiáº¿c",
                    ImageUrl = request.ImageUrl,
                    IsFeatured = request.IsFeatured,
                    StoreId = null // Set to null for now, will handle multi-store later
                };
                
                // Táº¡o sáº£n pháº©m
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                Console.WriteLine($"[CreateProduct] ÄÃ£ lÆ°u productId: {product.ProductId}");
                
                // Tá»± Ä‘á»™ng táº¡o inventory transaction náº¿u cÃ³ sá»‘ lÆ°á»£ng ban Ä‘áº§u
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
                        Reason = "Nháº­p kho ban Ä‘áº§u",
                        Notes = product.Description ?? "Táº¡o sáº£n pháº©m má»›i",
                        TransactionDate = DateTime.Now,
                        StockBefore = 0,
                        StockAfter = product.StockQuantity
                    };
                    
                    _context.InventoryTransactions.Add(inventoryTransaction);
                    await _context.SaveChangesAsync();
                    
                    Console.WriteLine($"[CreateProduct] ÄÃ£ táº¡o inventory transaction cho sáº£n pháº©m má»›i: {inventoryTransaction.TransactionId}");
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
        public async Task<ActionResult> GetProducts([FromQuery] int? storeId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? productGroupId = null, [FromQuery] string search = null, [FromQuery] bool? isActive = null, [FromQuery] string stockStatus = null)
        {
            var query = _context.Products.AsQueryable();
            
            // Filter by store if storeId is provided
            // Include products with null StoreId (shared products) and products belonging to the specific store
            if (storeId.HasValue)
            {
                query = query.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
            }
            
            // Filter by product group if provided
            if (productGroupId.HasValue)
            {
                query = query.Where(p => p.ProductGroupId == productGroupId.Value);
            }
            
            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.ToLower().Trim();
                query = query.Where(p => p.Name.ToLower().Contains(searchTerm) || 
                                       (p.Barcode != null && p.Barcode.ToLower().Contains(searchTerm)) ||
                                       (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
            }

            // Filter by active status if provided
            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }
            else
            {
                // Máº·c Ä‘á»‹nh chá»‰ hiá»ƒn thá»‹ sáº£n pháº©m active
                query = query.Where(p => p.IsActive == true);
            }

            // Filter by stock status if provided
            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                switch (stockStatus.ToLower())
                {
                    case "out-of-stock":
                        query = query.Where(p => p.StockQuantity == 0);
                        break;
                    case "low-stock":
                        query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.MinStockLevel);
                        break;
                    case "in-stock":
                        query = query.Where(p => p.StockQuantity > p.MinStockLevel);
                        break;
                    // "all" hoáº·c giÃ¡ trá»‹ khÃ¡c thÃ¬ khÃ´ng filter
                }
            }
            
            // Get total count for pagination
            var totalCount = await query.CountAsync();
            
            // Apply pagination with ProductGroup information
            var products = await query
                .Include(p => p.ProductGroup)
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new {
                    p.ProductId,
                    p.Name,
                    p.Description,
                    p.Barcode,
                    p.Price,
                    p.CostPrice,
                    p.ProductGroupId,
                    ProductGroupName = p.ProductGroup != null ? p.ProductGroup.Name : "ChÆ°a phÃ¢n loáº¡i",
                    p.StockQuantity,
                    p.MinStockLevel,
                    p.Unit,
                    p.ImageUrl,
                    p.IsFeatured,
                    p.IsActive,
                    p.StoreId,
                    StockStatus = p.StockQuantity == 0 ? "Háº¿t hÃ ng" : 
                                 p.StockQuantity <= p.MinStockLevel ? "Sáº¯p háº¿t" : "CÃ²n hÃ ng"
                })
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
        public async Task<ActionResult> GetFeaturedProducts([FromQuery] int? storeId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 9999)
        {
            var query = _context.Products.AsQueryable();
            
            // Filter by store if storeId is provided
            if (storeId.HasValue)
            {
                query = query.Where(p => p.StoreId == storeId.Value || p.StoreId == null);
            }
            
            var featuredProducts = await query
                .Where(p => p.IsFeatured && p.IsActive)
                .Include(p => p.ProductGroup)
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new {
                    p.ProductId,
                    p.Name,
                    p.Description,
                    p.Barcode,
                    p.Price,
                    p.CostPrice,
                    p.ProductGroupId,
                    ProductGroupName = p.ProductGroup != null ? p.ProductGroup.Name : "ChÆ°a phÃ¢n loáº¡i",
                    p.StockQuantity,
                    p.MinStockLevel,
                    p.Unit,
                    p.ImageUrl,
                    p.IsFeatured,
                    p.IsActive,
                    p.StoreId,
                    StockStatus = p.StockQuantity == 0 ? "Háº¿t hÃ ng" : 
                                 p.StockQuantity <= p.MinStockLevel ? "Sáº¯p háº¿t" : "CÃ²n hÃ ng"
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
                    return BadRequest(new { message = "ID khÃ´ng khá»›p" });
                }

                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct == null)
                {
                    Console.WriteLine($"[UpdateProduct] Product not found: {id}");
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                // Update properties (chá»‰ cáº­p nháº­t khi cÃ³ giÃ¡ trá»‹ má»›i)
                existingProduct.Name = updatedProduct.Name;
                existingProduct.Barcode = updatedProduct.Barcode;
                existingProduct.CategoryId = updatedProduct.CategoryId;
                existingProduct.ProductGroupId = updatedProduct.ProductGroupId;
                existingProduct.Price = updatedProduct.Price;
                existingProduct.CostPrice = updatedProduct.CostPrice;
                existingProduct.StockQuantity = updatedProduct.StockQuantity;
                existingProduct.MinStockLevel = updatedProduct.MinStockLevel;
                existingProduct.Unit = updatedProduct.Unit;
                
                // Chá»‰ cáº­p nháº­t ImageUrl khi cÃ³ giÃ¡ trá»‹ má»›i vÃ  khÃ´ng rá»—ng
                if (!string.IsNullOrEmpty(updatedProduct.ImageUrl))
                {
                    existingProduct.ImageUrl = updatedProduct.ImageUrl;
                }
                
                existingProduct.Description = updatedProduct.Description;
                existingProduct.IsFeatured = updatedProduct.IsFeatured;

                await _context.SaveChangesAsync();
                Console.WriteLine($"[UpdateProduct] Successfully updated product: {id}");
                
                return Ok(new { message = "Cáº­p nháº­t sáº£n pháº©m thÃ nh cÃ´ng", product = existingProduct });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Console.WriteLine($"[UpdateProduct] Concurrency error: {ex.Message}");
                if (!_context.Products.Any(e => e.ProductId == id))
                {
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }
                else
                {
                    return StatusCode(500, new { message = "Lá»—i Ä‘á»“ng thá»i", error = ex.Message });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UpdateProduct] Error: {ex.Message}");
                Console.WriteLine($"[UpdateProduct] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "KhÃ´ng thá»ƒ cáº­p nháº­t sáº£n pháº©m", error = ex.Message });
            }
        }

        // DELETE: api/products/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                Console.WriteLine($"[DeleteProduct] Deleting product ID: {id}");

                // TÃ¬m sáº£n pháº©m cáº§n xÃ³a
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    Console.WriteLine($"[DeleteProduct] Product not found: {id}");
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                // Kiá»ƒm tra xem sáº£n pháº©m cÃ³ Ä‘ang Ä‘Æ°á»£c sá»­ dá»¥ng trong Ä‘Æ¡n hÃ ng khÃ´ng
                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
                var hasInventoryTransactions = await _context.InventoryTransactions.AnyAsync(it => it.ProductId == id);
                
                if (hasOrders || hasInventoryTransactions)
                {
                    // KhÃ´ng thá»ƒ xÃ³a, chuyá»ƒn sang vÃ´ hiá»‡u hÃ³a
                    Console.WriteLine($"[DeleteProduct] Product {id} has constraints, deactivating instead of deleting");
                    product.IsActive = false;
                    await _context.SaveChangesAsync();
                    
                    string reason = hasOrders ? "cÃ³ trong Ä‘Æ¡n hÃ ng" : "cÃ³ lá»‹ch sá»­ giao dá»‹ch kho";
                    return Ok(new { 
                        message = $"Sáº£n pháº©m {reason}, Ä‘Ã£ vÃ´ hiá»‡u hÃ³a thay vÃ¬ xÃ³a. CÃ³ thá»ƒ khÃ´i phá»¥c láº¡i sau.", 
                        productId = id,
                        action = "deactivated"
                    });
                }

                // CÃ³ thá»ƒ xÃ³a hoÃ n toÃ n
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[DeleteProduct] Product deleted successfully: {id}");
                return Ok(new { message = "XÃ³a sáº£n pháº©m thÃ nh cÃ´ng", productId = id, action = "deleted" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DeleteProduct] Error: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi xÃ³a sáº£n pháº©m", error = ex.Message });
            }
        }

        // POST: api/products/{id}/adjust-stock
        [HttpPost("{id}/adjust-stock")]
        public async Task<IActionResult> AdjustStock(int id, [FromBody] StockAdjustmentRequest request)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Sáº£n pháº©m khÃ´ng tá»“n táº¡i");
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
                    Reason = request.Reason ?? "Äiá»u chá»‰nh tá»“n kho"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi cáº­p nháº­t tá»“n kho", error = ex.Message });
            }
        }

        // GET: api/products/{id}/stock-info
        [HttpGet("{id}/stock-info")]
        public async Task<ActionResult> GetProductStockInfo(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound("Sáº£n pháº©m khÃ´ng tá»“n táº¡i");
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
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                product.IsFeatured = !product.IsFeatured;
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = product.IsFeatured ? "ÄÃ£ thÃªm vÃ o sáº£n pháº©m hay bÃ¡n" : "ÄÃ£ xÃ³a khá»i sáº£n pháº©m hay bÃ¡n",
                    productId = product.ProductId,
                    productName = product.Name,
                    isFeatured = product.IsFeatured
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi cáº­p nháº­t tráº¡ng thÃ¡i sáº£n pháº©m", error = ex.Message });
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

                // Thiáº¿t láº­p headers
                var headers = new[] { 
                    "TÃªn sáº£n pháº©m (*)", 
                    "MÃ£ váº¡ch", 
                    "GiÃ¡ bÃ¡n (*)", 
                    "GiÃ¡ vá»‘n", 
                    "Sá»‘ lÆ°á»£ng tá»“n kho (*)", 
                    "Má»©c tá»“n kho tá»‘i thiá»ƒu", 
                    "ÄÆ¡n vá»‹", 
                    "MÃ´ táº£",
                    "ID NhÃ³m sáº£n pháº©m",
                    "TÃªn nhÃ³m sáº£n pháº©m",
                    "Sáº£n pháº©m hay bÃ¡n (0/1)"
                };

                // ThÃªm headers vÃ o hÃ ng Ä‘áº§u tiÃªn
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Láº¥y dá»¯ liá»‡u thá»±c táº¿ tá»« database
                var products = await _context.Products.ToListAsync(); // Láº¥y táº¥t cáº£ sáº£n pháº©m
                var productGroups = await _context.ProductGroups.ToListAsync();
                var groupDict = productGroups.ToDictionary(g => g.ProductGroupId, g => g.Name);

                // ThÃªm dá»¯ liá»‡u sáº£n pháº©m thá»±c táº¿ lÃ m vÃ­ dá»¥
                int row = 2;
                foreach (var product in products)
                {
                    worksheet.Cells[row, 1].Value = product.Name ?? "TÃªn sáº£n pháº©m";
                    worksheet.Cells[row, 2].Value = product.Barcode ?? "";
                    worksheet.Cells[row, 3].Value = (double)product.Price;
                    worksheet.Cells[row, 4].Value = product.CostPrice.HasValue ? (double)product.CostPrice.Value : 0;
                    worksheet.Cells[row, 5].Value = product.StockQuantity;
                    worksheet.Cells[row, 6].Value = product.MinStockLevel;
                    worksheet.Cells[row, 7].Value = product.Unit ?? "chiáº¿c";
                    worksheet.Cells[row, 8].Value = product.Description ?? "";
                    worksheet.Cells[row, 9].Value = product.ProductGroupId ?? 1;
                    worksheet.Cells[row, 10].Value = product.ProductGroupId.HasValue && groupDict.ContainsKey(product.ProductGroupId.Value) 
                        ? groupDict[product.ProductGroupId.Value] : "";
                    worksheet.Cells[row, 11].Value = product.IsFeatured ? 1 : 0;
                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();

                // Táº¡o sheet thá»© 2 cho danh sÃ¡ch nhÃ³m sáº£n pháº©m
                var groupSheet = package.Workbook.Worksheets.Add("Danh sÃ¡ch nhÃ³m sáº£n pháº©m");
                groupSheet.Cells[1, 1].Value = "ID NhÃ³m";
                groupSheet.Cells[1, 2].Value = "TÃªn nhÃ³m";
                groupSheet.Cells[1, 1].Style.Font.Bold = true;
                groupSheet.Cells[1, 2].Style.Font.Bold = true;
                groupSheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                groupSheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                groupSheet.Cells[1, 2].Style.Fill.PatternType = ExcelFillStyle.Solid;
                groupSheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);

                // ThÃªm danh sÃ¡ch nhÃ³m sáº£n pháº©m
                int groupRow = 2;
                foreach (var group in productGroups)
                {
                    groupSheet.Cells[groupRow, 1].Value = group.ProductGroupId;
                    groupSheet.Cells[groupRow, 2].Value = group.Name;
                    groupRow++;
                }
                groupSheet.Cells.AutoFitColumns();

                // Quay láº¡i sheet Ä‘áº§u tiÃªn vÃ  thÃªm ghi chÃº hÆ°á»›ng dáº«n
                worksheet.Select();
                int instructionRow = row + 1;
                worksheet.Cells[instructionRow, 1].Value = "HÆ¯á»šNG DáºªN:";
                worksheet.Cells[instructionRow, 1].Style.Font.Bold = true;
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- CÃ¡c cá»™t cÃ³ dáº¥u (*) lÃ  báº¯t buá»™c";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- GiÃ¡ bÃ¡n vÃ  GiÃ¡ vá»‘n nháº­p báº±ng sá»‘ (VNÄ)";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Sá»‘ lÆ°á»£ng tá»“n kho vÃ  Má»©c tá»“n kho tá»‘i thiá»ƒu nháº­p báº±ng sá»‘ nguyÃªn";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- ID NhÃ³m sáº£n pháº©m: xem sheet 'Danh sÃ¡ch nhÃ³m sáº£n pháº©m'";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- TÃªn nhÃ³m sáº£n pháº©m: náº¿u nháº­p tÃªn má»›i, há»‡ thá»‘ng sáº½ tá»± táº¡o nhÃ³m má»›i";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Náº¿u ID nhÃ³m khÃ´ng tá»“n táº¡i + cÃ³ tÃªn nhÃ³m: táº¡o nhÃ³m má»›i";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- CÃ³ thá»ƒ Ä‘á»ƒ trá»‘ng ID nhÃ³m vÃ  chá»‰ nháº­p tÃªn nhÃ³m Ä‘á»ƒ táº¡o nhÃ³m má»›i";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Sáº£n pháº©m hay bÃ¡n: nháº­p 1 (hay bÃ¡n) hoáº·c 0 (thÆ°á»ng)";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- CÃ¡c dÃ²ng dá»¯ liá»‡u máº«u phÃ­a trÃªn cÃ³ thá»ƒ sá»­a Ä‘á»•i hoáº·c xÃ³a";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- ThÃªm dÃ²ng má»›i phÃ­a dÆ°á»›i Ä‘á»ƒ nháº­p sáº£n pháº©m má»›i";

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"Products_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº¡o template Excel", error = ex.Message });
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
                var worksheet = package.Workbook.Worksheets.Add("Danh sÃ¡ch sáº£n pháº©m");

                // Thiáº¿t láº­p headers
                var headers = new[] {
                    "ID NhÃ³m sáº£n pháº©m",
                    "TÃªn nhÃ³m sáº£n pháº©m",
                    "MÃ£ váº¡ch",
                    "TÃªn sáº£n pháº©m",
                    "GiÃ¡ bÃ¡n",
                    "GiÃ¡ vá»‘n",
                    "Sá»‘ lÆ°á»£ng tá»“n kho",
                    "Má»©c tá»“n kho tá»‘i thiá»ƒu",
                    "ÄÆ¡n vá»‹",
                    "MÃ´ táº£",
                    "Sáº£n pháº©m hay bÃ¡n (0/1)"
                };

                // ThÃªm headers vÃ o hÃ ng Ä‘áº§u tiÃªn
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    worksheet.Cells[1, i + 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }

                // Láº¥y táº¥t cáº£ sáº£n pháº©m tá»« database
                var products = await _context.Products
                    .OrderBy(p => p.ProductId)
                    .ToListAsync();

                // Láº¥y danh sÃ¡ch nhÃ³m sáº£n pháº©m Ä‘á»ƒ mapping tÃªn
                var productGroups = await _context.ProductGroups.ToListAsync();
                var groupDict = productGroups.ToDictionary(g => g.ProductGroupId, g => g.Name);

                // ThÃªm dá»¯ liá»‡u sáº£n pháº©m
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
                    worksheet.Cells[row, 9].Value = product.Unit ?? "chiáº¿c";
                    worksheet.Cells[row, 10].Value = product.Description ?? "";
                    worksheet.Cells[row, 11].Value = product.IsFeatured ? 1 : 0;

                    // Äá»‹nh dáº¡ng cho cÃ¡c Ã´
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                    }

                    row++;
                }

                // Tá»± Ä‘á»™ng Ä‘iá»u chá»‰nh Ä‘á»™ rá»™ng cá»™t
                worksheet.Cells.AutoFitColumns();

                // Thiáº¿t láº­p chiá»u rá»™ng tá»‘i thiá»ƒu cho cÃ¡c cá»™t
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
                return StatusCode(500, new { message = "Lá»—i khi xuáº¥t dá»¯ liá»‡u sáº£n pháº©m", error = ex.Message });
            }
        }

        // POST: api/products/import-excel
        [HttpPost("import-excel")]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lÃ²ng chá»n file Excel Ä‘á»ƒ upload" });
            }

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
            {
                return BadRequest(new { message = "Chá»‰ há»— trá»£ file Excel (.xlsx, .xls)" });
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
                    return BadRequest(new { message = "File Excel khÃ´ng cÃ³ worksheet nÃ o" });
                }

                var rowCount = worksheet.Dimension?.Rows ?? 0;
                if (rowCount < 2)
                {
                    return BadRequest(new { message = "File Excel khÃ´ng cÃ³ dá»¯ liá»‡u Ä‘á»ƒ import" });
                }

                // Láº¥y danh sÃ¡ch ProductGroup Ä‘á»ƒ validate
                var productGroups = await _context.ProductGroups.ToListAsync();
                var validGroupIds = productGroups.Select(pg => pg.ProductGroupId).ToHashSet();

                // Äá»c tá»« hÃ ng 2 (bá» qua header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Äá»c dá»¯ liá»‡u tá»« cÃ¡c cá»™t
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

                        // Kiá»ƒm tra cÃ¡c trÆ°á»ng báº¯t buá»™c
                        if (string.IsNullOrEmpty(name))
                        {
                            errors.Add($"HÃ ng {row}: TÃªn sáº£n pháº©m khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng");
                            continue;
                        }

                        if (string.IsNullOrEmpty(priceText) || !decimal.TryParse(priceText, out var price) || price < 0)
                        {
                            errors.Add($"HÃ ng {row}: GiÃ¡ bÃ¡n khÃ´ng há»£p lá»‡");
                            continue;
                        }

                        if (string.IsNullOrEmpty(stockText) || !int.TryParse(stockText, out var stock) || stock < 0)
                        {
                            errors.Add($"HÃ ng {row}: Sá»‘ lÆ°á»£ng tá»“n kho khÃ´ng há»£p lá»‡");
                            continue;
                        }

                        // Xá»­ lÃ½ ProductGroup - tá»± Ä‘á»™ng táº¡o má»›i náº¿u cáº§n
                        int? finalProductGroupId = null;
                        
                        // TrÆ°á»ng há»£p 1: CÃ³ ID nhÃ³m sáº£n pháº©m
                        if (!string.IsNullOrEmpty(productGroupIdText) && int.TryParse(productGroupIdText, out var productGroupId))
                        {
                            if (validGroupIds.Contains(productGroupId))
                            {
                                // ID tá»“n táº¡i - sá»­ dá»¥ng ID nÃ y
                                finalProductGroupId = productGroupId;
                            }
                            else if (!string.IsNullOrEmpty(productGroupName))
                            {
                                // ID khÃ´ng tá»“n táº¡i nhÆ°ng cÃ³ tÃªn nhÃ³m - táº¡o nhÃ³m má»›i vá»›i tÃªn Ä‘Æ°á»£c cung cáº¥p
                                var existingGroup = productGroups.FirstOrDefault(g => g.Name.ToLower() == productGroupName.ToLower());
                                if (existingGroup != null)
                                {
                                    finalProductGroupId = existingGroup.ProductGroupId;
                                }
                                else
                                {
                                    // Táº¡o nhÃ³m sáº£n pháº©m má»›i
                                    var newGroup = new ProductGroup { Name = productGroupName };
                                    _context.ProductGroups.Add(newGroup);
                                    await _context.SaveChangesAsync();
                                    
                                    finalProductGroupId = newGroup.ProductGroupId;
                                    productGroups.Add(newGroup); // ThÃªm vÃ o danh sÃ¡ch Ä‘á»ƒ trÃ¡nh táº¡o trÃ¹ng
                                    validGroupIds.Add(newGroup.ProductGroupId);
                                }
                            }
                            else
                            {
                                // ID khÃ´ng tá»“n táº¡i vÃ  khÃ´ng cÃ³ tÃªn nhÃ³m
                                errors.Add($"HÃ ng {row}: ID NhÃ³m sáº£n pháº©m {productGroupId} khÃ´ng tá»“n táº¡i. Vui lÃ²ng cung cáº¥p tÃªn nhÃ³m Ä‘á»ƒ táº¡o má»›i.");
                                continue;
                            }
                        }
                        // TrÆ°á»ng há»£p 2: Chá»‰ cÃ³ tÃªn nhÃ³m - tÃ¬m hoáº·c táº¡o má»›i
                        else if (!string.IsNullOrEmpty(productGroupName))
                        {
                            var existingGroup = productGroups.FirstOrDefault(g => g.Name.ToLower() == productGroupName.ToLower());
                            if (existingGroup != null)
                            {
                                finalProductGroupId = existingGroup.ProductGroupId;
                            }
                            else
                            {
                                // Táº¡o nhÃ³m sáº£n pháº©m má»›i
                                var newGroup = new ProductGroup { Name = productGroupName };
                                _context.ProductGroups.Add(newGroup);
                                await _context.SaveChangesAsync();
                                
                                finalProductGroupId = newGroup.ProductGroupId;
                                productGroups.Add(newGroup); // ThÃªm vÃ o danh sÃ¡ch Ä‘á»ƒ trÃ¡nh táº¡o trÃ¹ng
                                validGroupIds.Add(newGroup.ProductGroupId);
                            }
                        }
                        // TrÆ°á»ng há»£p 3: KhÃ´ng cÃ³ cáº£ ID vÃ  tÃªn
                        else
                        {
                            errors.Add($"HÃ ng {row}: Pháº£i cÃ³ Ã­t nháº¥t ID nhÃ³m sáº£n pháº©m hoáº·c tÃªn nhÃ³m sáº£n pháº©m");
                            continue;
                        }

                        // Parse cÃ¡c trÆ°á»ng khÃ´ng báº¯t buá»™c
                        decimal.TryParse(costPriceText, out var costPrice);
                        int.TryParse(minStockText, out var minStock);
                        if (minStock <= 0) minStock = 5; // GiÃ¡ trá»‹ máº·c Ä‘á»‹nh
                        
                        // Parse IsFeatured (1 = hay bÃ¡n, 0 hoáº·c empty = thÆ°á»ng)
                        bool isFeatured = false;
                        if (!string.IsNullOrEmpty(isFeaturedText) && int.TryParse(isFeaturedText, out var featuredValue))
                        {
                            isFeatured = featuredValue == 1;
                        }

                        // Kiá»ƒm tra trÃ¹ng tÃªn sáº£n pháº©m
                        var existingProductByName = await _context.Products.FirstOrDefaultAsync(p => !string.IsNullOrEmpty(p.Name) && p.Name.ToLower() == name.ToLower());
                        if (existingProductByName != null)
                        {
                            skippedCount++;
                            importResults.Add(new
                            {
                                Row = row,
                                Name = name,
                                Status = "Skipped",
                                Reason = $"Sáº£n pháº©m Ä‘Ã£ tá»“n táº¡i (ID: {existingProductByName.ProductId})"
                            });
                            continue;
                        }

                        // Kiá»ƒm tra trÃ¹ng mÃ£ váº¡ch náº¿u cÃ³
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
                                    Reason = $"MÃ£ váº¡ch Ä‘Ã£ tá»“n táº¡i"
                                });
                                continue;
                            }
                        }

                        // Táº¡o sáº£n pháº©m má»›i
                        var product = new Product
                        {
                            Name = name,
                            Barcode = string.IsNullOrEmpty(barcode) ? null : barcode,
                            Price = price,
                            CostPrice = costPrice > 0 ? costPrice : null,
                            StockQuantity = stock,
                            MinStockLevel = minStock,
                            Unit = string.IsNullOrEmpty(unit) ? "chiáº¿c" : unit,
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
                        errors.Add($"HÃ ng {row}: Lá»—i khi táº¡o sáº£n pháº©m - {ex.Message}");
                    }
                }

                return Ok(new
                {
                    Message = $"Import hoÃ n táº¥t. ThÃ nh cÃ´ng: {successCount}, Bá» qua: {skippedCount}, Lá»—i: {errors.Count}",
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
                return StatusCode(500, new { message = "Lá»—i khi xá»­ lÃ½ file Excel", error = ex.Message });
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
                    return BadRequest(new { message = "TÃªn sáº£n pháº©m lÃ  báº¯t buá»™c" });
                }

                var imageUrl = await _imageSearchService.SearchAndDownloadImageAsync(
                request.ProductName, 
                request.ProductGroupName, 
                request.Description
            );
                
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y hÃ¬nh áº£nh phÃ¹ há»£p" });
                }

                return Ok(new { imageUrl = imageUrl });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi tÃ¬m kiáº¿m hÃ¬nh áº£nh", error = ex.Message });
            }
        }

        [HttpPost("search-images")]
        public async Task<IActionResult> SearchImages([FromBody] ImageSearchRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ProductName))
                {
                    return BadRequest(new { message = "TÃªn sáº£n pháº©m lÃ  báº¯t buá»™c" });
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
                return StatusCode(500, new { message = "Lá»—i khi tÃ¬m kiáº¿m hÃ¬nh áº£nh", error = ex.Message });
            }
        }



        // PUT: api/products/{id}/toggle-active
        [HttpPut("{id}/toggle-active")]
        public async Task<IActionResult> ToggleProductActive(int id)
        {
            try
            {
                Console.WriteLine($"[ToggleProductActive] Toggling active status for product ID: {id}");

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    Console.WriteLine($"[ToggleProductActive] Product not found: {id}");
                    return NotFound(new { message = "Sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                // Toggle tráº¡ng thÃ¡i
                product.IsActive = !product.IsActive;
                await _context.SaveChangesAsync();

                string action = product.IsActive ? "KÃ­ch hoáº¡t" : "VÃ´ hiá»‡u hÃ³a";
                Console.WriteLine($"[ToggleProductActive] Product {id} {action.ToLower()} successfully");
                
                return Ok(new { 
                    message = $"{action} sáº£n pháº©m thÃ nh cÃ´ng", 
                    productId = id,
                    isActive = product.IsActive,
                    action = product.IsActive ? "activated" : "deactivated"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ToggleProductActive] Error: {ex.Message}");
                return StatusCode(500, new { message = "Lá»—i khi thay Ä‘á»•i tráº¡ng thÃ¡i sáº£n pháº©m", error = ex.Message });
            }
        }
    }

    // Model cho request Ä‘iá»u chá»‰nh tá»“n kho
    public class StockAdjustmentRequest
    {
        public int NewQuantity { get; set; }
        public string? Reason { get; set; }
    }

    // Model cho request táº¡o sáº£n pháº©m má»›i
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

    // Model cho request cáº­p nháº­t sáº£n pháº©m
    public class UpdateProductRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Barcode { get; set; }
        public decimal? Price { get; set; }
        public decimal? CostPrice { get; set; }
        public int? ProductGroupId { get; set; }
        public int? StockQuantity { get; set; }
        public int? MinStockLevel { get; set; }
        public string? Unit { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsFeatured { get; set; }
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

