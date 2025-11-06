using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public interface ILoyaltyService
    {
        Task<int> CalculatePointsForOrderAsync(int customerId, decimal orderAmount, int orderId);
        Task<bool> ProcessOrderPointsAsync(int orderId, bool isRefund = false);
        Task<decimal> GetPointsValueAsync(int points);
        Task<bool> CanRedeemPointsAsync(int customerId, int pointsToRedeem);
        Task<bool> RedeemPointsAsync(int customerId, int pointsToRedeem, int orderId, string reason);
        Task<bool> UpdateCustomerTierAsync(int customerId);
        Task<LoyaltyConfig> GetLoyaltyConfigAsync();
    }
}