using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<LoyaltyService> _logger;

        public LoyaltyService(AppDbContext context, ILogger<LoyaltyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<LoyaltyConfig> GetLoyaltyConfigAsync()
        {
            return await _context.LoyaltyConfigs.FirstOrDefaultAsync() 
                ?? new LoyaltyConfig 
                { 
                    IsEnabled = false, 
                    PointsPerCurrency = 1000, 
                    MinOrderAmountForPoints = 50000 
                };
        }

        public async Task<int> CalculatePointsForOrderAsync(int customerId, decimal orderAmount, int orderId)
        {
            try
            {
                var config = await GetLoyaltyConfigAsync();
                if (!config.IsEnabled || orderAmount < config.MinOrderAmountForPoints)
                    return 0;

                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return 0;

                // Base points calculation
                var basePoints = (int)(orderAmount / config.PointsPerCurrency);

                // Apply multipliers
                var multiplier = 1.0m;

                // Check happy hour
                if (config.HappyHourEnabled)
                {
                    var now = DateTime.Now.TimeOfDay;
                    if (now >= config.HappyHourStartTime && now <= config.HappyHourEndTime)
                    {
                        multiplier *= config.HappyHourMultiplier;
                    }
                }

                // Check weekend bonus
                if (config.WeekendBonusEnabled && (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday))
                {
                    multiplier *= config.WeekendMultiplier;
                }

                // Check birthday bonus
                if (config.BirthdayBonusEnabled && customer.DateOfBirth.HasValue)
                {
                    var today = DateTime.Now;
                    var birthday = customer.DateOfBirth.Value;
                    var daysDiff = Math.Abs((today - new DateTime(today.Year, birthday.Month, birthday.Day)).Days);
                    
                    if (daysDiff <= config.BirthdayValidDays)
                    {
                        multiplier *= config.BirthdayMultiplier;
                    }
                }

                var finalPoints = (int)(basePoints * multiplier);

                // Apply max points per order limit
                if (config.MaxPointsPerOrder.HasValue && finalPoints > config.MaxPointsPerOrder.Value)
                {
                    finalPoints = config.MaxPointsPerOrder.Value;
                }

                return finalPoints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating points for order {OrderId}, customer {CustomerId}", orderId, customerId);
                return 0;
            }
        }

        public async Task<bool> ProcessOrderPointsAsync(int orderId, bool isRefund = false)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null || order.CustomerId == null)
                {
                    _logger.LogWarning("Order {OrderId} not found or has no customer", orderId);
                    return false;
                }

                // Check if points already processed for this order
                var existingTransaction = await _context.LoyaltyTransactions
                    .AnyAsync(t => t.OrderId == orderId && t.TransactionType == (isRefund ? LoyaltyTransactionType.REDEEM : LoyaltyTransactionType.EARN));

                if (existingTransaction)
                {
                    _logger.LogInformation("Points already processed for order {OrderId}", orderId);
                    return true;
                }

                var points = await CalculatePointsForOrderAsync(order.CustomerId.Value, order.TotalAmount, orderId);
                
                if (points <= 0 && !isRefund)
                {
                    _logger.LogInformation("No points to award for order {OrderId}", orderId);
                    return true;
                }

                // Create loyalty transaction
                var transaction = new LoyaltyTransaction
                {
                    CustomerId = order.CustomerId.Value,
                    OrderId = orderId,
                    TransactionType = isRefund ? LoyaltyTransactionType.REDEEM : LoyaltyTransactionType.EARN,
                    Points = isRefund ? -points : points,
                    Reason = isRefund ? $"Refund for order #{order.OrderNumber}" : $"Purchase order #{order.OrderNumber}",
                    ExpiryDate = DateTime.Now.AddDays((await GetLoyaltyConfigAsync()).PointExpiryDays),
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = order.StaffId
                };

                // Update customer loyalty points
                var customer = order.Customer;
                customer.LoyaltyPoints += transaction.Points;
                customer.TotalSpent += isRefund ? -order.TotalAmount : order.TotalAmount;

                // Set balance after transaction
                transaction.PointsBalance = customer.LoyaltyPoints;

                _context.LoyaltyTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                // Update customer tier
                await UpdateCustomerTierAsync(customer.CustomerId);

                _logger.LogInformation("Processed {Points} points for order {OrderId}, customer {CustomerId}", 
                    transaction.Points, orderId, customer.CustomerId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing points for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<decimal> GetPointsValueAsync(int points)
        {
            var config = await GetLoyaltyConfigAsync();
            return points * config.PointValue;
        }

        public async Task<bool> CanRedeemPointsAsync(int customerId, int pointsToRedeem)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            return customer != null && customer.LoyaltyPoints >= pointsToRedeem;
        }

        public async Task<bool> RedeemPointsAsync(int customerId, int pointsToRedeem, int orderId, string reason)
        {
            try
            {
                if (!await CanRedeemPointsAsync(customerId, pointsToRedeem))
                    return false;

                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return false;

                var transaction = new LoyaltyTransaction
                {
                    CustomerId = customerId,
                    OrderId = orderId,
                    TransactionType = LoyaltyTransactionType.REDEEM,
                    Points = -pointsToRedeem,
                    Reason = reason,
                    ProcessedAt = DateTime.Now
                };

                customer.LoyaltyPoints -= pointsToRedeem;
                transaction.PointsBalance = customer.LoyaltyPoints;

                _context.LoyaltyTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Redeemed {Points} points for customer {CustomerId}", pointsToRedeem, customerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redeeming points for customer {CustomerId}", customerId);
                return false;
            }
        }

        public async Task<bool> UpdateCustomerTierAsync(int customerId)
        {
            try
            {
                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return false;

                var tier = await _context.CustomerTiers
                    .Where(t => t.IsActive && 
                               customer.TotalSpent >= t.MinSpent && 
                               customer.LoyaltyPoints >= t.MinPoints)
                    .OrderByDescending(t => t.MinSpent)
                    .ThenByDescending(t => t.MinPoints)
                    .FirstOrDefaultAsync();

                if (tier != null)
                {
                    customer.TierId = tier.TierId;
                    customer.HangKhachHang = tier.TierName switch
                    {
                        "VIP" => CustomerRank.VIP,
                        "Premium" => CustomerRank.Premium, 
                        "Platinum" => CustomerRank.Platinum,
                        _ => CustomerRank.Thuong
                    };
                    
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated customer {CustomerId} to tier {TierName}", customerId, tier.TierName);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tier for customer {CustomerId}", customerId);
                return false;
            }
        }
    }
}