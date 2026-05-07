using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductGroupsController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public ProductGroupsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductGroup>>> GetProductGroups()
        {
            try
            {
                Console.WriteLine("Getting product groups...");
                var groups = await _context.ProductGroups.ToListAsync();
                Console.WriteLine($"Found {groups.Count} product groups");
                return groups;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting product groups: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y danh sÃ¡ch nhÃ³m sáº£n pháº©m", error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateProductGroup([FromBody] ProductGroup group)
        {
            try
            {
                Console.WriteLine($"Creating product group: {group.Name}");
                
                if (string.IsNullOrEmpty(group.Name))
                {
                    return BadRequest(new { message = "TÃªn nhÃ³m sáº£n pháº©m lÃ  báº¯t buá»™c" });
                }

                _context.ProductGroups.Add(group);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"Product group created with ID: {group.ProductGroupId}");
                return CreatedAtAction(nameof(GetProductGroup), new { id = group.ProductGroupId }, group);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating product group: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "KhÃ´ng thá»ƒ táº¡o nhÃ³m sáº£n pháº©m", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductGroup>> GetProductGroup(int id)
        {
            var group = await _context.ProductGroups.FindAsync(id);
            if (group == null) return NotFound();
            return group;
        }

        // DELETE: api/productgroups/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductGroup(int id, [FromQuery] bool force = false)
        {
            try
            {
                var group = await _context.ProductGroups.FindAsync(id);
                if (group == null)
                {
                    return NotFound(new { message = "NhÃ³m sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                // Láº¥y danh sÃ¡ch sáº£n pháº©m trong nhÃ³m
                var productsInGroup = await _context.Products.Where(p => p.ProductGroupId == id).ToListAsync();
                
                if (productsInGroup.Count > 0 && !force)
                {
                    return BadRequest(new { 
                        message = $"NhÃ³m nÃ y cÃ²n {productsInGroup.Count} sáº£n pháº©m. Báº¡n cÃ³ muá»‘n xÃ³a cÆ°á»¡ng bá»©c khÃ´ng?",
                        productCount = productsInGroup.Count,
                        canForceDelete = true
                    });
                }

                if (force && productsInGroup.Count > 0)
                {
                    // XÃ³a cÆ°á»¡ng bá»©c: gÃ¡n sáº£n pháº©m vá» null hoáº·c xÃ³a sáº£n pháº©m
                    foreach (var product in productsInGroup)
                    {
                        // Kiá»ƒm tra xem sáº£n pháº©m cÃ³ thá»ƒ xÃ³a hoÃ n toÃ n khÃ´ng
                        var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == product.ProductId);
                        var hasInventoryTransactions = await _context.InventoryTransactions.AnyAsync(it => it.ProductId == product.ProductId);
                        
                        if (hasOrders || hasInventoryTransactions)
                        {
                            // Chá»‰ gÃ¡n vá» null náº¿u cÃ³ rÃ ng buá»™c
                            product.ProductGroupId = null;
                        }
                        else
                        {
                            // XÃ³a hoÃ n toÃ n náº¿u khÃ´ng cÃ³ rÃ ng buá»™c
                            _context.Products.Remove(product);
                        }
                    }
                }

                _context.ProductGroups.Remove(group);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Product group deleted: {id}, force: {force}, products handled: {productsInGroup.Count}");
                return Ok(new { 
                    message = force ? 
                        $"ÄÃ£ xÃ³a cÆ°á»¡ng bá»©c nhÃ³m sáº£n pháº©m vÃ  xá»­ lÃ½ {productsInGroup.Count} sáº£n pháº©m" : 
                        "XÃ³a nhÃ³m sáº£n pháº©m thÃ nh cÃ´ng",
                    productGroupId = id,
                    productsHandled = productsInGroup.Count,
                    forced = force
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product group: {ex.Message}");
                return StatusCode(500, new { message = "KhÃ´ng thá»ƒ xÃ³a nhÃ³m sáº£n pháº©m", error = ex.Message });
            }
        }

        // PUT: api/productgroups/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProductGroup(int id, [FromBody] ProductGroup updatedGroup)
        {
            try
            {
                if (id != updatedGroup.ProductGroupId)
                {
                    return BadRequest(new { message = "ID khÃ´ng khá»›p" });
                }

                var existingGroup = await _context.ProductGroups.FindAsync(id);
                if (existingGroup == null)
                {
                    return NotFound(new { message = "NhÃ³m sáº£n pháº©m khÃ´ng tá»“n táº¡i" });
                }

                // Kiá»ƒm tra trÃ¹ng tÃªn (trá»« chÃ­nh nÃ³)
                var duplicateName = await _context.ProductGroups
                    .AnyAsync(pg => pg.ProductGroupId != id && 
                             !string.IsNullOrEmpty(pg.Name) && 
                             pg.Name.ToLower() == updatedGroup.Name.ToLower());
                
                if (duplicateName)
                {
                    return BadRequest(new { message = "TÃªn nhÃ³m sáº£n pháº©m Ä‘Ã£ tá»“n táº¡i" });
                }

                existingGroup.Name = updatedGroup.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cáº­p nháº­t nhÃ³m sáº£n pháº©m thÃ nh cÃ´ng", productGroup = existingGroup });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product group: {ex.Message}");
                return StatusCode(500, new { message = "KhÃ´ng thá»ƒ cáº­p nháº­t nhÃ³m sáº£n pháº©m", error = ex.Message });
            }
        }

        // GET: api/productgroups/export-template
        [HttpGet("export-template")]
        public async Task<IActionResult> ExportTemplate()
        {
            try
            {
                // Set license cho EPPlus 5.x
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("ProductGroups Template");

                // Thiáº¿t láº­p headers
                var headers = new[] { 
                    "TÃªn nhÃ³m sáº£n pháº©m (*)"
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
                var productGroups = await _context.ProductGroups.Take(3).ToListAsync(); // Láº¥y 3 nhÃ³m máº«u

                // ThÃªm dá»¯ liá»‡u nhÃ³m sáº£n pháº©m thá»±c táº¿ lÃ m vÃ­ dá»¥
                int row = 2;
                foreach (var group in productGroups)
                {
                    worksheet.Cells[row, 1].Value = group.Name ?? "NhÃ³m sáº£n pháº©m máº«u";
                    row++;
                }

                // ThÃªm má»™t vÃ i dÃ²ng trá»‘ng cho ngÆ°á»i dÃ¹ng nháº­p liá»‡u
                for (int i = 0; i < 5; i++)
                {
                    worksheet.Cells[row + i, 1].Value = "";
                }

                // Auto fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // ThÃªm hÆ°á»›ng dáº«n
                int instructionRow = row + 7;
                worksheet.Cells[instructionRow, 1].Value = "HÆ¯á»šNG DáºªN:";
                worksheet.Cells[instructionRow, 1].Style.Font.Bold = true;
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- TÃªn nhÃ³m sáº£n pháº©m (*): báº¯t buá»™c";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- NhÃ³m trÃ¹ng tÃªn sáº½ bá»‹ bá» qua khi import";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- CÃ¡c dÃ²ng dá»¯ liá»‡u máº«u phÃ­a trÃªn cÃ³ thá»ƒ sá»­a Ä‘á»•i hoáº·c xÃ³a";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- ThÃªm dÃ²ng má»›i phÃ­a dÆ°á»›i Ä‘á»ƒ nháº­p nhÃ³m sáº£n pháº©m má»›i";

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"ProductGroups_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i khi táº¡o template Excel", error = ex.Message });
            }
        }

        // POST: api/productgroups/import-excel
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

                // Äá»c tá»« hÃ ng 2 (bá» qua header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Äá»c dá»¯ liá»‡u tá»« cá»™t
                        var name = worksheet.Cells[row, 1].Text?.Trim();

                        // Kiá»ƒm tra trÆ°á»ng báº¯t buá»™c
                        if (string.IsNullOrEmpty(name))
                        {
                            errors.Add($"HÃ ng {row}: TÃªn nhÃ³m sáº£n pháº©m khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng");
                            continue;
                        }

                        // Kiá»ƒm tra trÃ¹ng tÃªn nhÃ³m sáº£n pháº©m (khÃ´ng phÃ¢n biá»‡t hoa thÆ°á»ng)
                        var existingGroupByName = await _context.ProductGroups.FirstOrDefaultAsync(pg => !string.IsNullOrEmpty(pg.Name) && pg.Name.ToLower() == name.ToLower());
                        if (existingGroupByName != null)
                        {
                            skippedCount++;
                            importResults.Add(new
                            {
                                Row = row,
                                Name = name,
                                Status = "Skipped",
                                Reason = $"NhÃ³m sáº£n pháº©m Ä‘Ã£ tá»“n táº¡i (ID: {existingGroupByName.ProductGroupId})"
                            });
                            continue;
                        }

                        // Táº¡o nhÃ³m sáº£n pháº©m má»›i
                        var productGroup = new ProductGroup
                        {
                            Name = name
                        };

                        _context.ProductGroups.Add(productGroup);
                        await _context.SaveChangesAsync();

                        successCount++;
                        importResults.Add(new
                        {
                            Row = row,
                            ProductGroupId = productGroup.ProductGroupId,
                            Name = productGroup.Name,
                            Status = "Success"
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"HÃ ng {row}: Lá»—i khi táº¡o nhÃ³m sáº£n pháº©m - {ex.Message}");
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
    }
}

