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

        public async Task<LoyaltySettings?> GetLoyaltySettingsAsync()
        {
            return await _context.LoyaltySettings.FirstOrDefaultAsync() 
                ?? new LoyaltySettings 
                { 
                    IsPointsEnabled = false, 
                    PointsRate = 1000, 
                    MinOrderAmount = 50000 
                };
        }

        public async Task<int> CalculatePointsForOrderAsync(int customerId, decimal orderAmount, int orderId)
        {
            try
            {
                var settings = await GetLoyaltySettingsAsync();
                if (!settings.IsPointsEnabled || orderAmount < settings.MinOrderAmount)
                    return 0;

                var customer = await _context.Customers.FindAsync(customerId);
                if (customer == null) return 0;

                // Base points calculation using LoyaltySettings
                var basePoints = (int)(orderAmount / settings.PointsRate);

                // Apply customer tier multiplier
                var multiplier = 1.0m;
                var bonusPoints = 0;
                var customerTier = await _context.CustomerTiers.FindAsync(customer.TierId);
                if (customerTier != null)
                {
                    multiplier = customerTier.PointsMultiplier;
                    
                    // Bổ sung quyền lợi điểm thưởng theo hạng
                    bonusPoints = CalculateTierBonusPoints(customerTier.TierName, orderAmount, basePoints);
                }

                // Fallback to old config for advanced features (happy hour, weekend, birthday)
                var config = await GetLoyaltyConfigAsync();
                
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

                var finalPoints = (int)(basePoints * multiplier) + bonusPoints;

                // Apply max points per order limit from settings
                if (settings.MaxPointsPerOrder > 0 && finalPoints > settings.MaxPointsPerOrder)
                {
                    finalPoints = settings.MaxPointsPerOrder;
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
                
                // Áp dụng điểm thưởng ngày đặc biệt (sinh nhật, holiday)
                if (!isRefund && points > 0)
                {
                    var specialBonus = await CalculateSpecialDayBonusAsync(order.Customer, points);
                    if (specialBonus > 0)
                    {
                        points += specialBonus;
                        _logger.LogInformation("Applied special day bonus: +{SpecialBonus} points for order {OrderId}", specialBonus, orderId);
                    }
                }
                
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
                    ExpiryDate = DateTime.Now.AddDays((await GetLoyaltySettingsAsync()).PointsExpirationDays),
                    ProcessedAt = DateTime.Now,
                    ProcessedBy = order.StaffId
                };

                // Update customer loyalty points
                var customer = order.Customer;
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found in order {OrderId}", orderId);
                    return false;
                }
                
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
            var settings = await GetLoyaltySettingsAsync();
            return points * settings.RedemptionRate;
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
                var customer = await _context.Customers
                    .Include(c => c.CustomerTier)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);
                    
                if (customer == null) return false;

                // Tính lại tổng chi tiêu và điểm từ database
                var totalSpent = await _context.Orders
                    .Where(o => o.CustomerId == customerId && o.Status == "completed")
                    .SumAsync(o => o.TotalAmount);

                var totalPoints = await _context.LoyaltyTransactions
                    .Where(t => t.CustomerId == customerId)
                    .SumAsync(t => t.Points);

                // Cập nhật thông tin khách hàng
                customer.TotalSpent = totalSpent;
                customer.LoyaltyPoints = totalPoints;

                // Tìm hạng phù hợp nhất
                var appropriateTier = await _context.CustomerTiers
                    .Where(t => t.IsActive && 
                               totalSpent >= t.MinSpent && 
                               totalPoints >= t.MinPoints)
                    .OrderByDescending(t => t.MinSpent)
                    .ThenByDescending(t => t.MinPoints)
                    .FirstOrDefaultAsync();

                // Nếu không tìm thấy hạng nào phù hợp, lấy hạng thấp nhất
                if (appropriateTier == null)
                {
                    appropriateTier = await _context.CustomerTiers
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.MinSpent)
                        .ThenBy(t => t.MinPoints)
                        .FirstOrDefaultAsync();
                }

                bool tierChanged = false;
                var oldTierName = customer.CustomerTier?.TierName ?? "Chưa xác định";

                if (appropriateTier != null && customer.TierId != appropriateTier.TierId)
                {
                    var oldTierId = customer.TierId;
                    customer.TierId = appropriateTier.TierId;
                    
                    // Cập nhật enum HangKhachHang theo tên tier
                    customer.HangKhachHang = appropriateTier.TierName switch
                    {
                        "Kim cương" => CustomerRank.VIP,
                        "Vàng" => CustomerRank.Platinum,
                        "Bạc" => CustomerRank.Premium,
                        "Đồng" => CustomerRank.Thuong,
                        _ => CustomerRank.Thuong
                    };

                    tierChanged = true;
                    
                    // Log tier upgrade
                    _logger.LogInformation("Customer {CustomerId} tier updated from {OldTier} to {NewTier}. Total spent: {TotalSpent}, Total points: {TotalPoints}", 
                        customerId, oldTierName, appropriateTier.TierName, totalSpent, totalPoints);

                    // Tạo notification cho khách hàng về việc nâng hạng
                    if (oldTierId.HasValue && oldTierId != appropriateTier.TierId)
                    {
                        var notification = new Notification
                        {
                            CustomerId = customerId,
                            Title = "🎉 Chúc mừng bạn đã được nâng hạng!",
                            Message = $"Chúc mừng bạn đã được nâng hạng từ {oldTierName} lên {appropriateTier.TierName}! " +
                                     $"Bạn sẽ được hưởng {appropriateTier.DiscountPercentage}% giảm giá và nhận thêm {appropriateTier.PointsMultiplier}x điểm thưởng.",
                            Type = NotificationType.SystemAlert,
                            Status = NotificationStatus.Unread,
                            CreatedAt = DateTime.Now
                        };
                        _context.Notifications.Add(notification);
                    }
                }
                    
                await _context.SaveChangesAsync();

                if (tierChanged)
                {
                    _logger.LogInformation("Successfully updated customer {CustomerId} to tier {TierName} (ID: {TierId})", 
                        customerId, appropriateTier?.TierName, appropriateTier?.TierId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tier for customer {CustomerId}", customerId);
                return false;
            }
        }

        public async Task<object> GetCustomerLoyaltyStatusAsync(int customerId)
        {
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.CustomerTier)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId);

                if (customer == null)
                    return new { error = "Không tìm thấy khách hàng" };

                // Tính tổng chi tiêu và điểm hiện tại
                var totalSpent = await _context.Orders
                    .Where(o => o.CustomerId == customerId && o.Status == "completed")
                    .SumAsync(o => o.TotalAmount);

                var totalPoints = await _context.LoyaltyTransactions
                    .Where(t => t.CustomerId == customerId)
                    .SumAsync(t => t.Points);

                // Tìm hạng hiện tại phù hợp
                var currentTier = await _context.CustomerTiers
                    .Where(t => t.IsActive && totalSpent >= t.MinSpent && totalPoints >= t.MinPoints)
                    .OrderByDescending(t => t.MinSpent)
                    .ThenByDescending(t => t.MinPoints)
                    .FirstOrDefaultAsync();

                if (currentTier == null)
                {
                    currentTier = await _context.CustomerTiers
                        .Where(t => t.IsActive)
                        .OrderBy(t => t.MinSpent)
                        .FirstOrDefaultAsync();
                }

                // Tìm hạng tiếp theo
                var nextTier = await _context.CustomerTiers
                    .Where(t => t.IsActive && (t.MinSpent > totalSpent || t.MinPoints > totalPoints))
                    .OrderBy(t => t.MinSpent)
                    .ThenBy(t => t.MinPoints)
                    .FirstOrDefaultAsync();

                // Tính toán để đạt hạng tiếp theo
                decimal spentToNext = 0;
                int pointsToNext = 0;
                decimal progressPercentage = 100;

                if (nextTier != null)
                {
                    spentToNext = Math.Max(0, nextTier.MinSpent - totalSpent);
                    pointsToNext = Math.Max(0, nextTier.MinPoints - totalPoints);
                    
                    if (nextTier.MinSpent > 0)
                    {
                        progressPercentage = Math.Min(100, (totalSpent / nextTier.MinSpent) * 100);
                    }
                }

                return new
                {
                    customerId = customerId,
                    customerName = customer.HoTen,
                    totalSpent = totalSpent,
                    totalPoints = totalPoints,
                    currentTier = currentTier == null ? null : new
                    {
                        tierId = currentTier.TierId,
                        tierName = currentTier.TierName,
                        tierColor = currentTier.TierColor,
                        discountPercentage = currentTier.DiscountPercentage,
                        pointsMultiplier = currentTier.PointsMultiplier,
                        description = currentTier.Description
                    },
                    nextTier = nextTier == null ? null : new
                    {
                        tierId = nextTier.TierId,
                        tierName = nextTier.TierName,
                        tierColor = nextTier.TierColor,
                        discountPercentage = nextTier.DiscountPercentage,
                        pointsMultiplier = nextTier.PointsMultiplier,
                        description = nextTier.Description,
                        minSpent = nextTier.MinSpent,
                        minPoints = nextTier.MinPoints
                    },
                    progress = new
                    {
                        spentToNext = spentToNext,
                        pointsToNext = pointsToNext,
                        progressPercentage = Math.Round(progressPercentage, 2)
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting loyalty status for customer {CustomerId}", customerId);
                return new { error = "Lỗi khi lấy thông tin tích điểm" };
            }
        }

        public async Task<bool> CheckAndUpdateAllCustomerTiersAsync()
        {
            try
            {
                var customers = await _context.Customers
                    .Where(c => c.IsActive)
                    .ToListAsync();

                int updatedCount = 0;
                foreach (var customer in customers)
                {
                    var updated = await UpdateCustomerTierAsync(customer.CustomerId);
                    if (updated) updatedCount++;
                }

                _logger.LogInformation("Updated tiers for {UpdatedCount} out of {TotalCount} customers", updatedCount, customers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all customer tiers");
                return false;
            }
        }

        /// <summary>
        /// Tính toán điểm thưởng bổ sung dựa trên hạng khách hàng
        /// </summary>
        /// <param name="tierName">Tên hạng khách hàng</param>
        /// <param name="orderAmount">Số tiền đơn hàng</param>
        /// <param name="basePoints">Điểm cơ bản đã tính</param>
        /// <returns>Điểm thưởng bổ sung</returns>
        private int CalculateTierBonusPoints(string tierName, decimal orderAmount, int basePoints)
        {
            var bonusPoints = 0;
            var tierNameLower = tierName.ToLower();

            // Quyền lợi điểm thưởng theo hạng
            if (tierNameLower.Contains("bạc") || tierNameLower.Contains("silver"))
            {
                // Hạng Bạc: +20% điểm thưởng + 50 điểm cố định mỗi đơn >= 100k
                bonusPoints = (int)(basePoints * 0.2m);
                if (orderAmount >= 100000) bonusPoints += 50;
                
                _logger.LogInformation("Tier Bạc bonus: +{BonusPercent}% + {FixedBonus} points", 20, orderAmount >= 100000 ? 50 : 0);
            }
            else if (tierNameLower.Contains("vàng") || tierNameLower.Contains("gold"))
            {
                // Hạng Vàng: +50% điểm thưởng + 100 điểm cố định mỗi đơn >= 200k + điểm milestone
                bonusPoints = (int)(basePoints * 0.5m);
                if (orderAmount >= 200000) bonusPoints += 100;
                
                // Milestone bonus cho hạng Vàng: mỗi 500k thêm 200 điểm
                var milestoneBonus = (int)(orderAmount / 500000) * 200;
                bonusPoints += milestoneBonus;
                
                _logger.LogInformation("Tier Vàng bonus: +50% + {FixedBonus} + {MilestoneBonus} milestone points", 
                    orderAmount >= 200000 ? 100 : 0, milestoneBonus);
            }
            else if (tierNameLower.Contains("kim cương") || tierNameLower.Contains("diamond") || 
                     tierNameLower.Contains("platinum"))
            {
                // Hạng Kim cương/Platinum: +100% điểm thưởng + 300 điểm cố định + điểm milestone + weekend double
                bonusPoints = (int)(basePoints * 1.0m); // Gấp đôi điểm cơ bản
                bonusPoints += 300; // Điểm cố định cao
                
                // Milestone bonus cho hạng Kim cương: mỗi 300k thêm 250 điểm
                var milestoneBonus = (int)(orderAmount / 300000) * 250;
                bonusPoints += milestoneBonus;
                
                // Weekend double bonus (chỉ hạng Kim cương)
                var isWeekend = DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday;
                if (isWeekend)
                {
                    var weekendBonus = bonusPoints; // Gấp đôi toàn bộ bonus
                    bonusPoints += weekendBonus;
                    _logger.LogInformation("Weekend double bonus applied for Diamond tier: +{WeekendBonus} points", weekendBonus);
                }
                
                _logger.LogInformation("Tier Kim cương bonus: +100% + 300 fixed + {MilestoneBonus} milestone points", milestoneBonus);
            }

            return bonusPoints;
        }

        /// <summary>
        /// Tính toán điểm thưởng đặc biệt cho ngày đặc biệt (sinh nhật, holiday)
        /// </summary>
        /// <param name="customer">Thông tin khách hàng</param>
        /// <param name="basePoints">Điểm cơ bản</param>
        /// <returns>Điểm thưởng đặc biệt</returns>
        private async Task<int> CalculateSpecialDayBonusAsync(Customer customer, int basePoints)
        {
            var specialBonus = 0;
            var today = DateTime.Now;
            
            // Bonus sinh nhật (áp dụng trong vòng 7 ngày)
            if (customer.DateOfBirth.HasValue)
            {
                var birthday = new DateTime(today.Year, customer.DateOfBirth.Value.Month, customer.DateOfBirth.Value.Day);
                var daysDiff = Math.Abs((today - birthday).Days);
                
                if (daysDiff <= 7) // Trong vòng 7 ngày sinh nhật
                {
                    var customerTier = await _context.CustomerTiers.FindAsync(customer.TierId);
                    var tierName = customerTier?.TierName?.ToLower() ?? "";
                    
                    if (tierName.Contains("bạc") || tierName.Contains("silver"))
                        specialBonus = basePoints; // Gấp đôi điểm
                    else if (tierName.Contains("vàng") || tierName.Contains("gold"))
                        specialBonus = (int)(basePoints * 1.5m); // Gấp 2.5 lần điểm
                    else if (tierName.Contains("kim cương") || tierName.Contains("diamond") || tierName.Contains("platinum"))
                        specialBonus = basePoints * 2; // Gấp 3 lần điểm
                    
                    _logger.LogInformation("Birthday bonus applied: +{BonusPoints} points for customer {CustomerId}", specialBonus, customer.CustomerId);
                }
            }
            
            // Bonus ngày lễ (1/1, 30/4, 2/9, Tết...)
            if (IsHolidayToday(today))
            {
                var holidayBonus = (int)(basePoints * 0.5m); // +50% điểm trong ngày lễ
                specialBonus += holidayBonus;
                _logger.LogInformation("Holiday bonus applied: +{HolidayBonus} points", holidayBonus);
            }
            
            return specialBonus;
        }

        /// <summary>
        /// Kiểm tra có phải ngày lễ không
        /// </summary>
        private bool IsHolidayToday(DateTime date)
        {
            // Ngày lễ cố định
            var holidays = new[]
            {
                new DateTime(date.Year, 1, 1),   // Tết Dương lịch
                new DateTime(date.Year, 4, 30),  // 30/4
                new DateTime(date.Year, 5, 1),   // 1/5
                new DateTime(date.Year, 9, 2),   // Quốc khánh
                new DateTime(date.Year, 12, 25), // Noel
            };
            
            return holidays.Contains(date.Date);
        }
    }
}