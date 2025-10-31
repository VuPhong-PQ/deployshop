using RetailPointBackend.Models;
using System.Text.Json;

namespace RetailPointBackend.Services
{
    public interface INotificationService
    {
        Task CreateNewOrderNotificationAsync(int orderId, string customerName, decimal totalAmount);
        Task CreateLowStockNotificationAsync(int productId, string productName, int currentStock, int minLevel);
        Task CreatePaymentSuccessNotificationAsync(int orderId, decimal amount, string paymentMethod);
        Task CreateOutOfStockNotificationAsync(int productId, string productName);
        Task CheckLowStockForAllProductsAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateNewOrderNotificationAsync(int orderId, string customerName, decimal totalAmount)
        {
            var notification = new Notification
            {
                Type = NotificationType.NewOrder,
                Title = "Đơn hàng mới",
                Message = $"Khách hàng {customerName} vừa đặt đơn hàng #{orderId}",
                OrderId = orderId,
                Metadata = JsonSerializer.Serialize(new
                {
                    CustomerName = customerName,
                    TotalAmount = totalAmount,
                    FormattedTotal = totalAmount.ToString("N0") + "đ"
                })
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateLowStockNotificationAsync(int productId, string productName, int currentStock, int minLevel)
        {
            // Kiểm tra xem đã có thông báo tồn kho thấp cho sản phẩm này trong 24h qua chưa
            var yesterday = DateTime.Now.AddDays(-1);
            var existingNotification = _context.Notifications
                .Where(n => n.Type == NotificationType.LowStock && 
                           n.ProductId == productId && 
                           n.CreatedAt > yesterday)
                .FirstOrDefault();

            if (existingNotification != null)
                return; // Đã có thông báo rồi, không tạo nữa

            var notification = new Notification
            {
                Type = NotificationType.LowStock,
                Title = "Cảnh báo tồn kho",
                Message = $"Hết cà phê Espresso sắp hết hàng",
                ProductId = productId,
                Metadata = JsonSerializer.Serialize(new
                {
                    ProductName = productName,
                    CurrentStock = currentStock,
                    MinLevel = minLevel
                })
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreatePaymentSuccessNotificationAsync(int orderId, decimal amount, string paymentMethod)
        {
            var notification = new Notification
            {
                Type = NotificationType.PaymentSuccess,
                Title = "Thanh toán thành công",
                Message = $"Đơn hàng #{orderId} đã được thanh toán",
                OrderId = orderId,
                Metadata = JsonSerializer.Serialize(new
                {
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    FormattedAmount = amount.ToString("N0") + "đ"
                })
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateOutOfStockNotificationAsync(int productId, string productName)
        {
            var notification = new Notification
            {
                Type = NotificationType.OutOfStock,
                Title = "Hết hàng",
                Message = $"Sản phẩm {productName} đã hết hàng",
                ProductId = productId,
                Metadata = JsonSerializer.Serialize(new
                {
                    ProductName = productName
                })
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CheckLowStockForAllProductsAsync()
        {
            var lowStockProducts = _context.Products
                .Where(p => p.StockQuantity <= p.MinStockLevel && p.StockQuantity > 0)
                .ToList();

            foreach (var product in lowStockProducts)
            {
                await CreateLowStockNotificationAsync(
                    product.ProductId, 
                    product.Name ?? "Sản phẩm không tên", 
                    product.StockQuantity, 
                    product.MinStockLevel);
            }

            var outOfStockProducts = _context.Products
                .Where(p => p.StockQuantity <= 0)
                .ToList();

            foreach (var product in outOfStockProducts)
            {
                await CreateOutOfStockNotificationAsync(
                    product.ProductId,
                    product.Name ?? "Sản phẩm không tên");
            }
        }
    }
}