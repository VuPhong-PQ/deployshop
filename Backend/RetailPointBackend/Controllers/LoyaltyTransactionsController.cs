using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LoyaltyTransactionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LoyaltyTransactionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/LoyaltyTransactions/customer/5
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult> GetCustomerTransactions(int customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var transactions = await _context.LoyaltyTransactions
                    .Where(t => t.CustomerId == customerId)
                    .Include(t => t.Order)
                    .Include(t => t.ProcessedByStaff)
                    .OrderByDescending(t => t.ProcessedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var totalCount = await _context.LoyaltyTransactions
                    .CountAsync(t => t.CustomerId == customerId);

                return Ok(new
                {
                    transactions = transactions,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/LoyaltyTransactions/process-order-points
        [HttpPost("process-order-points")]
        public async Task<ActionResult> ProcessOrderPoints([FromBody] ProcessOrderPointsRequest request)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .ThenInclude(c => c!.CustomerTier)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y Ä‘Æ¡n hÃ ng" });
                }

                if (order.CustomerId == null)
                {
                    return BadRequest(new { message = "ÄÆ¡n hÃ ng khÃ´ng cÃ³ thÃ´ng tin khÃ¡ch hÃ ng" });
                }

                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                if (config == null || !config.IsEnabled)
                {
                    return BadRequest(new { message = "Há»‡ thá»‘ng tÃ­ch Ä‘iá»ƒm chÆ°a Ä‘Æ°á»£c kÃ­ch hoáº¡t" });
                }

                // Kiá»ƒm tra Ä‘Ã£ tÃ­ch Ä‘iá»ƒm cho Ä‘Æ¡n hÃ ng nÃ y chÆ°a
                var existingTransaction = await _context.LoyaltyTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == request.OrderId && t.TransactionType == LoyaltyTransactionType.EARN);

                if (existingTransaction != null)
                {
                    return BadRequest(new { message = "ÄÆ¡n hÃ ng nÃ y Ä‘Ã£ Ä‘Æ°á»£c tÃ­ch Ä‘iá»ƒm" });
                }

                // TÃ­nh Ä‘iá»ƒm
                var points = await CalculateOrderPoints(order, config);
                if (points <= 0)
                {
                    return Ok(new { message = "ÄÆ¡n hÃ ng khÃ´ng Ä‘á»§ Ä‘iá»u kiá»‡n tÃ­ch Ä‘iá»ƒm", points = 0 });
                }

                // Cáº­p nháº­t Ä‘iá»ƒm khÃ¡ch hÃ ng
                order.Customer!.LoyaltyPoints += points;

                // Táº¡o transaction
                var transaction = new LoyaltyTransaction
                {
                    CustomerId = order.CustomerId.Value,
                    OrderId = order.OrderId,
                    TransactionType = LoyaltyTransactionType.EARN,
                    Points = points,
                    PointsBalance = order.Customer.LoyaltyPoints,
                    Reason = $"TÃ­ch Ä‘iá»ƒm tá»« Ä‘Æ¡n hÃ ng #{order.OrderNumber}",
                    ExpiryDate = DateTime.Now.AddDays(config.PointExpiryDays),
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = request.StaffId
                };

                _context.LoyaltyTransactions.Add(transaction);

                // Kiá»ƒm tra vÃ  cáº­p nháº­t cáº¥p Ä‘á»™ khÃ¡ch hÃ ng
                await UpdateCustomerTier(order.Customer);

                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "TÃ­ch Ä‘iá»ƒm thÃ nh cÃ´ng",
                    points = points,
                    newBalance = order.Customer.LoyaltyPoints,
                    transactionId = transaction.TransactionId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        // POST: api/LoyaltyTransactions/redeem-points
        [HttpPost("redeem-points")]
        public async Task<ActionResult> RedeemPoints([FromBody] RedeemPointsRequest request)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(request.CustomerId);
                if (customer == null)
                {
                    return NotFound(new { message = "KhÃ´ng tÃ¬m tháº¥y khÃ¡ch hÃ ng" });
                }

                var config = await _context.LoyaltyConfigs.FirstOrDefaultAsync();
                if (config == null || !config.AllowPointRedemption)
                {
                    return BadRequest(new { message = "Há»‡ thá»‘ng khÃ´ng cho phÃ©p Ä‘á»•i Ä‘iá»ƒm" });
                }

                if (customer.LoyaltyPoints < request.PointsToRedeem)
                {
                    return BadRequest(new { message = "KhÃ´ng Ä‘á»§ Ä‘iá»ƒm Ä‘á»ƒ thá»±c hiá»‡n giao dá»‹ch" });
                }

                // TÃ­nh giÃ¡ trá»‹ quy Ä‘á»•i
                var redeemValue = request.PointsToRedeem * config.PointValue / 100;

                // Cáº­p nháº­t Ä‘iá»ƒm khÃ¡ch hÃ ng
                customer.LoyaltyPoints -= request.PointsToRedeem;

                // Táº¡o transaction
                var transaction = new LoyaltyTransaction
                {
                    CustomerId = request.CustomerId,
                    OrderId = request.OrderId,
                    TransactionType = LoyaltyTransactionType.REDEEM,
                    Points = -request.PointsToRedeem,
                    PointsBalance = customer.LoyaltyPoints,
                    Reason = $"Äá»•i {request.PointsToRedeem} Ä‘iá»ƒm = {redeemValue:C0}",
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = request.StaffId
                };

                _context.LoyaltyTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "Äá»•i Ä‘iá»ƒm thÃ nh cÃ´ng",
                    pointsRedeemed = request.PointsToRedeem,
                    redeemValue = redeemValue,
                    newBalance = customer.LoyaltyPoints
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lá»—i server", error = ex.Message });
            }
        }

        private async Task<int> CalculateOrderPoints(Order order, LoyaltyConfig config)
        {
            if (order.TotalAmount < config.MinOrderAmountForPoints)
            {
                return 0;
            }

            var basePoints = Math.Floor(order.TotalAmount / config.PointsPerCurrency);
            var multiplier = 1.0m;

            // Kiá»ƒm tra Happy Hour
            if (config.HappyHourEnabled)
            {
                var orderTime = order.CreatedAt.TimeOfDay;
                if (orderTime >= config.HappyHourStartTime && orderTime <= config.HappyHourEndTime)
                {
                    multiplier *= config.HappyHourMultiplier;
                }
            }

            // Kiá»ƒm tra Weekend Bonus
            if (config.WeekendBonusEnabled && 
                (order.CreatedAt.DayOfWeek == DayOfWeek.Saturday || order.CreatedAt.DayOfWeek == DayOfWeek.Sunday))
            {
                multiplier *= config.WeekendMultiplier;
            }

            // Kiá»ƒm tra Birthday Bonus
            if (config.BirthdayBonusEnabled && order.Customer?.DateOfBirth.HasValue == true)
            {
                var birthday = order.Customer.DateOfBirth.Value;
                var daysDiff = Math.Abs((order.CreatedAt - birthday.AddYears(order.CreatedAt.Year - birthday.Year)).Days);
                
                if (daysDiff <= config.BirthdayValidDays)
                {
                    multiplier *= config.BirthdayMultiplier;
                }
            }

            // Ãp dá»¥ng há»‡ sá»‘ cáº¥p Ä‘á»™ khÃ¡ch hÃ ng
            if (order.Customer?.CustomerTier != null)
            {
                multiplier *= order.Customer.CustomerTier.PointsMultiplier;
            }

            var finalPoints = (int)Math.Floor(basePoints * multiplier);

            // Ãp dá»¥ng giá»›i háº¡n Ä‘iá»ƒm tá»‘i Ä‘a
            if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
            {
                finalPoints = config.MaxPointsPerOrder.Value;
            }

            return finalPoints;
        }

        private async Task UpdateCustomerTier(Customer customer)
        {
            // TÃ­nh tá»•ng chi tiÃªu
            var totalSpent = await _context.Orders
                .Where(o => o.CustomerId == customer.CustomerId && o.Status == "completed")
                .SumAsync(o => o.TotalAmount);

            // TÃ¬m cáº¥p Ä‘á»™ phÃ¹ há»£p (chá»n tier cao nháº¥t mÃ  khÃ¡ch hÃ ng Ä‘Ã¡p á»©ng Ä‘iá»u kiá»‡n)
            var appropriateTier = await _context.CustomerTiers
                .Where(t => t.IsActive && totalSpent >= t.MinSpent && customer.LoyaltyPoints >= t.MinPoints)
                .OrderByDescending(t => t.MinSpent)
                .ThenByDescending(t => t.MinPoints)
                .FirstOrDefaultAsync();

            if (appropriateTier != null && customer.TierId != appropriateTier.TierId)
            {
                customer.TierId = appropriateTier.TierId;
                
                // Cáº­p nháº­t enum HangKhachHang theo tÃªn tier (mapping Ä‘Ãºng thá»© tá»±)
                customer.HangKhachHang = appropriateTier.TierName switch
                {
                    "Kim cÆ°Æ¡ng" => CustomerRank.Platinum,  // Cao nháº¥t (3)
                    "VÃ ng" => CustomerRank.VIP,            // Cao (2)
                    "Báº¡c" => CustomerRank.Premium,         // Trung bÃ¬nh (1)
                    "Äá»“ng" => CustomerRank.Thuong,        // Tháº¥p nháº¥t (0)
                    _ => CustomerRank.Thuong
                };
            }
        }
    }

    public class ProcessOrderPointsRequest
    {
        public int OrderId { get; set; }
        public int? StaffId { get; set; }
    }

    public class RedeemPointsRequest
    {
        public int CustomerId { get; set; }
        public int PointsToRedeem { get; set; }
        public int? OrderId { get; set; }
        public int? StaffId { get; set; }
    }
}
