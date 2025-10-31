using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public interface IDiscountService
    {
        Task<List<Discount>> GetAvailableDiscountsAsync();
        Task<List<Discount>> GetDiscountsForProductAsync(int productId);
        Task<decimal> CalculateDiscountAmountAsync(int discountId, List<OrderItem> items, decimal orderTotal);
        Task<bool> CanApplyDiscountAsync(int discountId, List<OrderItem> items, decimal orderTotal);
        Task<OrderDiscount> ApplyDiscountToOrderAsync(int orderId, int discountId, List<OrderItem> items, decimal orderTotal, int? staffId = null);
        Task<OrderDiscount> ApplyDiscountToOrderItemAsync(int orderId, int orderItemId, int discountId, int? staffId = null);
        Task RemoveDiscountFromOrderAsync(int orderId, int discountId);
        Task<decimal> CalculateOrderTotalWithDiscountsAsync(List<OrderItem> items, List<OrderDiscount> discounts);
    }

    public class DiscountService : IDiscountService
    {
        private readonly AppDbContext _context;

        public DiscountService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Discount>> GetAvailableDiscountsAsync()
        {
            var now = DateTime.Now;
            return await _context.Discounts
                .Where(d => d.IsActive && !d.IsDeleted &&
                           (d.StartDate == null || d.StartDate <= now) &&
                           (d.EndDate == null || d.EndDate >= now) &&
                           (d.MaxUsage == null || d.UsageCount < d.MaxUsage))
                .Include(d => d.Product)
                .Include(d => d.Category)
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<List<Discount>> GetDiscountsForProductAsync(int productId)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return new List<Discount>();

            var now = DateTime.Now;
            return await _context.Discounts
                .Where(d => d.IsActive && !d.IsDeleted &&
                           (d.StartDate == null || d.StartDate <= now) &&
                           (d.EndDate == null || d.EndDate >= now) &&
                           (d.MaxUsage == null || d.UsageCount < d.MaxUsage) &&
                           (d.ProductId == productId || d.CategoryId == product.CategoryId || 
                            (d.ProductId == null && d.CategoryId == null)))
                .OrderBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<decimal> CalculateDiscountAmountAsync(int discountId, List<OrderItem> items, decimal orderTotal)
        {
            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount == null || !await CanApplyDiscountAsync(discountId, items, orderTotal))
                return 0;

            switch (discount.Type)
            {
                case DiscountType.PercentageTotal:
                    return Math.Round(orderTotal * (discount.Value / 100), 2);

                case DiscountType.FixedAmountTotal:
                    return Math.Min(discount.Value, orderTotal);

                case DiscountType.FixedAmountItem:
                    if (discount.ProductId.HasValue)
                    {
                        var applicableItems = items.Where(i => i.ProductId == discount.ProductId.Value);
                        var totalQuantity = applicableItems.Sum(i => i.Quantity);
                        var maxDiscount = applicableItems.Sum(i => i.TotalPrice);
                        return Math.Min(discount.Value * totalQuantity, maxDiscount);
                    }
                    else if (discount.CategoryId.HasValue)
                    {
                        var applicableItems = items.Where(i => i.Product?.CategoryId == discount.CategoryId.Value);
                        var totalQuantity = applicableItems.Sum(i => i.Quantity);
                        var maxDiscount = applicableItems.Sum(i => i.TotalPrice);
                        return Math.Min(discount.Value * totalQuantity, maxDiscount);
                    }
                    return 0;

                default:
                    return 0;
            }
        }

        public async Task<bool> CanApplyDiscountAsync(int discountId, List<OrderItem> items, decimal orderTotal)
        {
            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount == null || !discount.IsActive || discount.IsDeleted)
                return false;

            var now = DateTime.Now;
            if (discount.StartDate.HasValue && discount.StartDate > now)
                return false;

            if (discount.EndDate.HasValue && discount.EndDate < now)
                return false;

            if (discount.MaxUsage.HasValue && discount.UsageCount >= discount.MaxUsage)
                return false;

            // Kiểm tra điều kiện giá trị đơn hàng tối thiểu
            if (discount.MinOrderValue.HasValue && orderTotal < discount.MinOrderValue)
                return false;

            // Kiểm tra điều kiện số lượng tối thiểu
            if (discount.MinQuantity.HasValue)
            {
                if (discount.ProductId.HasValue)
                {
                    var productQuantity = items.Where(i => i.ProductId == discount.ProductId.Value)
                                               .Sum(i => i.Quantity);
                    if (productQuantity < discount.MinQuantity)
                        return false;
                }
                else if (discount.CategoryId.HasValue)
                {
                    var categoryQuantity = items.Where(i => i.Product?.CategoryId == discount.CategoryId.Value)
                                                .Sum(i => i.Quantity);
                    if (categoryQuantity < discount.MinQuantity)
                        return false;
                }
                else
                {
                    var totalQuantity = items.Sum(i => i.Quantity);
                    if (totalQuantity < discount.MinQuantity)
                        return false;
                }
            }

            return true;
        }

        public async Task<OrderDiscount> ApplyDiscountToOrderAsync(int orderId, int discountId, List<OrderItem> items, decimal orderTotal, int? staffId = null)
        {
            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount == null || !await CanApplyDiscountAsync(discountId, items, orderTotal))
                throw new InvalidOperationException("Không thể áp dụng giảm giá này");

            var discountAmount = await CalculateDiscountAmountAsync(discountId, items, orderTotal);

            var orderDiscount = new OrderDiscount
            {
                OrderId = orderId,
                DiscountId = discountId,
                DiscountName = discount.Name,
                DiscountType = discount.Type,
                DiscountValue = discount.Value,
                DiscountAmount = discountAmount,
                AppliedAt = DateTime.Now,
                AppliedBy = staffId
            };

            _context.OrderDiscounts.Add(orderDiscount);

            // Cập nhật usage count
            discount.UsageCount++;
            _context.Discounts.Update(discount);

            await _context.SaveChangesAsync();
            return orderDiscount;
        }

        public async Task<OrderDiscount> ApplyDiscountToOrderItemAsync(int orderId, int orderItemId, int discountId, int? staffId = null)
        {
            var orderItem = await _context.OrderItems
                .Include(oi => oi.Product)
                .FirstOrDefaultAsync(oi => oi.OrderItemId == orderItemId);

            if (orderItem == null)
                throw new InvalidOperationException("Không tìm thấy item trong đơn hàng");

            var discount = await _context.Discounts.FindAsync(discountId);
            if (discount == null || discount.Type != DiscountType.FixedAmountItem)
                throw new InvalidOperationException("Giảm giá không hợp lệ cho item");

            // Kiểm tra xem discount có áp dụng được cho item này không
            var isApplicable = (discount.ProductId == null && discount.CategoryId == null) ||
                              (discount.ProductId == orderItem.ProductId) ||
                              (discount.CategoryId == orderItem.Product?.CategoryId);

            if (!isApplicable)
                throw new InvalidOperationException("Giảm giá không áp dụng cho sản phẩm này");

            var maxDiscountAmount = orderItem.TotalPrice;
            var discountAmount = Math.Min(discount.Value * orderItem.Quantity, maxDiscountAmount);

            var orderDiscount = new OrderDiscount
            {
                OrderId = orderId,
                DiscountId = discountId,
                OrderItemId = orderItemId,
                DiscountName = discount.Name,
                DiscountType = discount.Type,
                DiscountValue = discount.Value,
                DiscountAmount = discountAmount,
                AppliedAt = DateTime.Now,
                AppliedBy = staffId
            };

            _context.OrderDiscounts.Add(orderDiscount);

            // Cập nhật discount amount cho order item
            orderItem.DiscountAmount = discountAmount;
            orderItem.FinalPrice = orderItem.TotalPrice - discountAmount;
            _context.OrderItems.Update(orderItem);

            // Cập nhật usage count
            discount.UsageCount++;
            _context.Discounts.Update(discount);

            await _context.SaveChangesAsync();
            return orderDiscount;
        }

        public async Task RemoveDiscountFromOrderAsync(int orderId, int discountId)
        {
            var orderDiscounts = await _context.OrderDiscounts
                .Where(od => od.OrderId == orderId && od.DiscountId == discountId)
                .ToListAsync();

            if (orderDiscounts.Any())
            {
                // Giảm usage count
                var discount = await _context.Discounts.FindAsync(discountId);
                if (discount != null)
                {
                    discount.UsageCount = Math.Max(0, discount.UsageCount - orderDiscounts.Count);
                    _context.Discounts.Update(discount);
                }

                // Reset discount cho order items nếu có
                foreach (var od in orderDiscounts.Where(od => od.OrderItemId.HasValue))
                {
                    var orderItem = await _context.OrderItems.FindAsync(od.OrderItemId.Value);
                    if (orderItem != null)
                    {
                        orderItem.DiscountAmount = 0;
                        orderItem.FinalPrice = orderItem.TotalPrice;
                        _context.OrderItems.Update(orderItem);
                    }
                }

                _context.OrderDiscounts.RemoveRange(orderDiscounts);
                await _context.SaveChangesAsync();
            }
        }

        public Task<decimal> CalculateOrderTotalWithDiscountsAsync(List<OrderItem> items, List<OrderDiscount> discounts)
        {
            var subtotal = items.Sum(i => i.TotalPrice);
            var totalDiscount = discounts.Sum(d => d.DiscountAmount);
            return Task.FromResult(Math.Max(0, subtotal - totalDiscount));
        }
    }
}