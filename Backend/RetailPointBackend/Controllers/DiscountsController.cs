using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IDiscountService _discountService;

        public DiscountsController(AppDbContext context, IDiscountService discountService)
        {
            _context = context;
            _discountService = discountService;
        }

        // GET: api/Discounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Discount>>> GetDiscounts()
        {
            var discounts = await _context.Discounts
                .Include(d => d.Product)
                .Include(d => d.Category)
                .Include(d => d.CreatedByStaff)
                .Where(d => !d.IsDeleted)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Ok(discounts);
        }

        // GET: api/Discounts/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<Discount>>> GetAvailableDiscounts()
        {
            var discounts = await _discountService.GetAvailableDiscountsAsync();
            return Ok(discounts);
        }

        // GET: api/Discounts/product/{productId}
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<Discount>>> GetDiscountsForProduct(int productId)
        {
            var discounts = await _discountService.GetDiscountsForProductAsync(productId);
            return Ok(discounts);
        }

        // GET: api/Discounts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Discount>> GetDiscount(int id)
        {
            var discount = await _context.Discounts
                .Include(d => d.Product)
                .Include(d => d.Category)
                .Include(d => d.CreatedByStaff)
                .FirstOrDefaultAsync(d => d.DiscountId == id && !d.IsDeleted);

            if (discount == null)
                return NotFound();

            return Ok(discount);
        }

        // POST: api/Discounts
        [HttpPost]
        public async Task<ActionResult<Discount>> CreateDiscount(CreateDiscountRequest request)
        {
            var discount = new Discount
            {
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Value = request.Value,
                MinOrderValue = request.MinOrderValue,
                MinQuantity = request.MinQuantity,
                ProductId = request.ProductId,
                CategoryId = request.CategoryId,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MaxUsage = request.MaxUsage,
                CreatedBy = request.CreatedBy,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UsageCount = 0
            };

            _context.Discounts.Add(discount);
            await _context.SaveChangesAsync();

            var createdDiscount = await _context.Discounts
                .Include(d => d.Product)
                .Include(d => d.Category)
                .Include(d => d.CreatedByStaff)
                .FirstOrDefaultAsync(d => d.DiscountId == discount.DiscountId);

            return CreatedAtAction(nameof(GetDiscount), new { id = discount.DiscountId }, createdDiscount);
        }

        // PUT: api/Discounts/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDiscount(int id, UpdateDiscountRequest request)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null || discount.IsDeleted)
                return NotFound();

            discount.Name = request.Name;
            discount.Description = request.Description;
            discount.Type = request.Type;
            discount.Value = request.Value;
            discount.MinOrderValue = request.MinOrderValue;
            discount.MinQuantity = request.MinQuantity;
            discount.ProductId = request.ProductId;
            discount.CategoryId = request.CategoryId;
            discount.StartDate = request.StartDate;
            discount.EndDate = request.EndDate;
            discount.MaxUsage = request.MaxUsage;
            discount.IsActive = request.IsActive;
            discount.UpdatedAt = DateTime.Now;

            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Discounts/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDiscount(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null || discount.IsDeleted)
                return NotFound();

            discount.IsDeleted = true;
            discount.UpdatedAt = DateTime.Now;

            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Discounts/calculate
        [HttpPost("calculate")]
        public async Task<ActionResult<CalculateDiscountResponse>> CalculateDiscount(CalculateDiscountRequest request)
        {
            try
            {
                var canApply = await _discountService.CanApplyDiscountAsync(request.DiscountId, request.Items, request.OrderTotal);
                
                if (!canApply)
                {
                    return Ok(new CalculateDiscountResponse
                    {
                        CanApply = false,
                        DiscountAmount = 0,
                        Message = "Không thể áp dụng giảm giá này"
                    });
                }

                var discountAmount = await _discountService.CalculateDiscountAmountAsync(request.DiscountId, request.Items, request.OrderTotal);
                
                return Ok(new CalculateDiscountResponse
                {
                    CanApply = true,
                    DiscountAmount = discountAmount,
                    FinalTotal = request.OrderTotal - discountAmount
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST: api/Discounts/toggle-status/{id}
        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleDiscountStatus(int id)
        {
            var discount = await _context.Discounts.FindAsync(id);
            if (discount == null || discount.IsDeleted)
                return NotFound();

            discount.IsActive = !discount.IsActive;
            discount.UpdatedAt = DateTime.Now;

            _context.Discounts.Update(discount);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTOs
    public class CreateDiscountRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType Type { get; set; }
        public decimal Value { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int? MinQuantity { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxUsage { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class UpdateDiscountRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType Type { get; set; }
        public decimal Value { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int? MinQuantity { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxUsage { get; set; }
        public bool IsActive { get; set; }
    }

    public class CalculateDiscountRequest
    {
        public int DiscountId { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public decimal OrderTotal { get; set; }
    }

    public class CalculateDiscountResponse
    {
        public bool CanApply { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalTotal { get; set; }
        public string? Message { get; set; }
    }
}