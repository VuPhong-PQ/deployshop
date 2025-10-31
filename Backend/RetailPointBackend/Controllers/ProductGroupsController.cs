using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace RetailPointBackend.Controllers
{
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
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách nhóm sản phẩm", error = ex.Message });
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
                    return BadRequest(new { message = "Tên nhóm sản phẩm là bắt buộc" });
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
                return StatusCode(500, new { message = "Không thể tạo nhóm sản phẩm", error = ex.Message });
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
        public async Task<IActionResult> DeleteProductGroup(int id)
        {
            try
            {
                var group = await _context.ProductGroups.FindAsync(id);
                if (group == null)
                {
                    return NotFound(new { message = "Nhóm sản phẩm không tồn tại" });
                }

                // Kiểm tra xem có sản phẩm nào thuộc nhóm này không
                var productsInGroup = await _context.Products.CountAsync(p => p.ProductGroupId == id);
                if (productsInGroup > 0)
                {
                    return BadRequest(new { message = $"Không thể xóa nhóm này vì còn {productsInGroup} sản phẩm. Hãy di chuyển sản phẩm trước khi xóa." });
                }

                _context.ProductGroups.Remove(group);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Product group deleted: {id}");
                return NoContent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product group: {ex.Message}");
                return StatusCode(500, new { message = "Không thể xóa nhóm sản phẩm", error = ex.Message });
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
                    return BadRequest(new { message = "ID không khớp" });
                }

                var existingGroup = await _context.ProductGroups.FindAsync(id);
                if (existingGroup == null)
                {
                    return NotFound(new { message = "Nhóm sản phẩm không tồn tại" });
                }

                // Kiểm tra trùng tên (trừ chính nó)
                var duplicateName = await _context.ProductGroups
                    .AnyAsync(pg => pg.ProductGroupId != id && 
                             !string.IsNullOrEmpty(pg.Name) && 
                             pg.Name.ToLower() == updatedGroup.Name.ToLower());
                
                if (duplicateName)
                {
                    return BadRequest(new { message = "Tên nhóm sản phẩm đã tồn tại" });
                }

                existingGroup.Name = updatedGroup.Name;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cập nhật nhóm sản phẩm thành công", productGroup = existingGroup });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product group: {ex.Message}");
                return StatusCode(500, new { message = "Không thể cập nhật nhóm sản phẩm", error = ex.Message });
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

                // Thiết lập headers
                var headers = new[] { 
                    "Tên nhóm sản phẩm (*)"
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
                var productGroups = await _context.ProductGroups.Take(3).ToListAsync(); // Lấy 3 nhóm mẫu

                // Thêm dữ liệu nhóm sản phẩm thực tế làm ví dụ
                int row = 2;
                foreach (var group in productGroups)
                {
                    worksheet.Cells[row, 1].Value = group.Name ?? "Nhóm sản phẩm mẫu";
                    row++;
                }

                // Thêm một vài dòng trống cho người dùng nhập liệu
                for (int i = 0; i < 5; i++)
                {
                    worksheet.Cells[row + i, 1].Value = "";
                }

                // Auto fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Thêm hướng dẫn
                int instructionRow = row + 7;
                worksheet.Cells[instructionRow, 1].Value = "HƯỚNG DẪN:";
                worksheet.Cells[instructionRow, 1].Style.Font.Bold = true;
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Tên nhóm sản phẩm (*): bắt buộc";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Nhóm trùng tên sẽ bị bỏ qua khi import";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Các dòng dữ liệu mẫu phía trên có thể sửa đổi hoặc xóa";
                instructionRow++;
                worksheet.Cells[instructionRow, 1].Value = "- Thêm dòng mới phía dưới để nhập nhóm sản phẩm mới";

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = $"ProductGroups_Template_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo template Excel", error = ex.Message });
            }
        }

        // POST: api/productgroups/import-excel
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

                // Đọc từ hàng 2 (bỏ qua header)
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        // Đọc dữ liệu từ cột
                        var name = worksheet.Cells[row, 1].Text?.Trim();

                        // Kiểm tra trường bắt buộc
                        if (string.IsNullOrEmpty(name))
                        {
                            errors.Add($"Hàng {row}: Tên nhóm sản phẩm không được để trống");
                            continue;
                        }

                        // Kiểm tra trùng tên nhóm sản phẩm (không phân biệt hoa thường)
                        var existingGroupByName = await _context.ProductGroups.FirstOrDefaultAsync(pg => !string.IsNullOrEmpty(pg.Name) && pg.Name.ToLower() == name.ToLower());
                        if (existingGroupByName != null)
                        {
                            skippedCount++;
                            importResults.Add(new
                            {
                                Row = row,
                                Name = name,
                                Status = "Skipped",
                                Reason = $"Nhóm sản phẩm đã tồn tại (ID: {existingGroupByName.ProductGroupId})"
                            });
                            continue;
                        }

                        // Tạo nhóm sản phẩm mới
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
                        errors.Add($"Hàng {row}: Lỗi khi tạo nhóm sản phẩm - {ex.Message}");
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
    }
}
