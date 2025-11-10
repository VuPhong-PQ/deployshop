using System.ComponentModel.DataAnnotations;
using RetailPointBackend.DTOs;

namespace RetailPointBackend.Validators
{
    public class CustomerTierValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not CustomerTierDto tier)
                return false;

            // Basic validation
            if (tier.MinSpent < 0)
            {
                ErrorMessage = "Chi tiêu tối thiểu không được âm";
                return false;
            }

            if (tier.MinPoints < 0)
            {
                ErrorMessage = "Điểm tối thiểu không được âm";
                return false;
            }

            if (tier.PointsMultiplier <= 0 || tier.PointsMultiplier > 10)
            {
                ErrorMessage = "Hệ số điểm phải từ 0.1 đến 10";
                return false;
            }

            if (tier.DiscountPercentage < 0 || tier.DiscountPercentage > 100)
            {
                ErrorMessage = "Phần trăm giảm giá từ 0 đến 100";
                return false;
            }

            // Color validation
            if (!IsValidHexColor(tier.TierColor))
            {
                ErrorMessage = "Màu sắc phải ở định dạng hex (#RRGGBB)";
                return false;
            }

            return true;
        }

        private static bool IsValidHexColor(string color)
        {
            if (string.IsNullOrEmpty(color))
                return false;

            if (!color.StartsWith("#"))
                return false;

            if (color.Length != 7)
                return false;

            return color[1..].All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
        }
    }

    public class TierConfigurationValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not List<CustomerTierDto> tiers)
                return false;

            var errors = new List<string>();

            // Check for duplicate names
            var duplicateNames = tiers
                .GroupBy(t => t.TierName.ToLower().Trim())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            if (duplicateNames.Any())
            {
                errors.Add($"Tên hạng trùng lặp: {string.Join(", ", duplicateNames)}");
            }

            // Check for logical progression
            var sortedTiers = tiers.OrderBy(t => t.MinSpent).ToList();
            
            for (int i = 1; i < sortedTiers.Count; i++)
            {
                var current = sortedTiers[i];
                var previous = sortedTiers[i - 1];

                // Check if higher tier has better benefits
                if (current.PointsMultiplier < previous.PointsMultiplier)
                {
                    errors.Add($"Hạng '{current.TierName}' có hệ số điểm thấp hơn hạng '{previous.TierName}'");
                }

                if (current.DiscountPercentage < previous.DiscountPercentage)
                {
                    errors.Add($"Hạng '{current.TierName}' có giảm giá thấp hơn hạng '{previous.TierName}'");
                }

                // Check minimum spending progression
                if (current.MinSpent == previous.MinSpent && current.MinPoints <= previous.MinPoints)
                {
                    errors.Add($"Hạng '{current.TierName}' và '{previous.TierName}' có điều kiện trùng lặp");
                }
            }

            if (errors.Any())
            {
                ErrorMessage = string.Join("; ", errors);
                return false;
            }

            return true;
        }
    }

    public static class TierConfigurationValidator
    {
        public static (bool IsValid, List<string> Errors, List<string> Warnings) ValidateConfiguration(List<CustomerTierDto> tiers)
        {
            var errors = new List<string>();
            var warnings = new List<string>();

            if (!tiers.Any())
            {
                errors.Add("Phải có ít nhất một hạng khách hàng");
                return (false, errors, warnings);
            }

            // Individual tier validation
            foreach (var tier in tiers)
            {
                var tierValidator = new CustomerTierValidationAttribute();
                if (!tierValidator.IsValid(tier))
                {
                    errors.Add($"Hạng '{tier.TierName}': {tierValidator.ErrorMessage}");
                }
            }

            // Overall configuration validation
            var configValidator = new TierConfigurationValidationAttribute();
            if (!configValidator.IsValid(tiers))
            {
                errors.Add(configValidator.ErrorMessage ?? "Cấu hình không hợp lệ");
            }

            // Warnings for better UX
            var sortedTiers = tiers.OrderBy(t => t.MinSpent).ToList();
            
            // Check for reasonable gaps between tiers
            for (int i = 1; i < sortedTiers.Count; i++)
            {
                var current = sortedTiers[i];
                var previous = sortedTiers[i - 1];

                var spentGapRatio = previous.MinSpent > 0 ? current.MinSpent / previous.MinSpent : current.MinSpent;
                if (spentGapRatio > 10)
                {
                    warnings.Add($"Khoảng cách chi tiêu giữa hạng '{previous.TierName}' và '{current.TierName}' có thể quá lớn");
                }

                if (spentGapRatio < 2 && current.MinSpent > 0)
                {
                    warnings.Add($"Khoảng cách chi tiêu giữa hạng '{previous.TierName}' và '{current.TierName}' có thể quá nhỏ");
                }
            }

            // Check for missing base tier (0 spent)
            if (!tiers.Any(t => t.MinSpent == 0))
            {
                warnings.Add("Nên có một hạng cơ bản với chi tiêu tối thiểu = 0 cho khách hàng mới");
            }

            return (errors.Count == 0, errors, warnings);
        }
    }
}