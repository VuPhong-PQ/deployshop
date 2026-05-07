using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    public class CategoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentId { get; set; }
        public bool IsVisible { get; set; }
    }

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

    // Test endpoint
    [HttpGet("test")]
    public IActionResult TestCategories()
    {
        try
        {
            return Ok(new { message = "Categories controller working!", timestamp = DateTime.Now });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

    // Test DB connection
    [HttpGet("testdb")]
    public async Task<IActionResult> TestDatabaseConnection()
    {
        try
        {
            var count = await _context.Categories.CountAsync();
            return Ok(new { message = "Database connection OK", categoryCount = count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Database error: {ex.Message}");
        }
    }

    // GET: api/Categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
    {
        try
        {
            var categories = await _context.Categories.ToListAsync();
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.Name ?? string.Empty,
                Description = c.Description,
                ParentId = c.ParentId,
                IsVisible = c.IsVisible
            }).ToList();
            
            return Ok(categoryDtos);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal error: {ex.Message}");
        }
    }        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryDto categoryDto)
        {
            var category = new Category
            {
                Name = categoryDto.CategoryName,
                Description = categoryDto.Description,
                ParentId = categoryDto.ParentId,
                IsVisible = categoryDto.IsVisible
            };
            
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            
            var resultDto = new CategoryDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.Name ?? string.Empty,
                Description = category.Description,
                ParentId = category.ParentId,
                IsVisible = category.IsVisible
            };
            
            return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, resultDto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDto>> GetCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();
            
            var categoryDto = new CategoryDto
            {
                CategoryId = category.CategoryId,
                CategoryName = category.Name ?? string.Empty,
                Description = category.Description,
                ParentId = category.ParentId,
                IsVisible = category.IsVisible
            };
            
            return Ok(categoryDto);
        }

        // Sá»­a nhÃ³m sáº£n pháº©m
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDto categoryDto)
        {
            if (id != categoryDto.CategoryId)
                return BadRequest();

            var existing = await _context.Categories.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = categoryDto.CategoryName;
            existing.Description = categoryDto.Description;
            existing.ParentId = categoryDto.ParentId;
            existing.IsVisible = categoryDto.IsVisible;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // XÃ³a nhÃ³m sáº£n pháº©m
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}

