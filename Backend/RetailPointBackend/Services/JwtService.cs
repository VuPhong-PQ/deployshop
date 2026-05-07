using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RetailPointBackend.Models;

namespace RetailPointBackend.Services
{
    public interface IJwtService
    {
        string GenerateToken(Staff staff, List<string> permissions);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;

        public JwtService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(Staff staff, List<string> permissions)
        {
            var secretKey = _configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = _configuration["Jwt:Issuer"] ?? "RetailPointBackend";
            var audience = _configuration["Jwt:Audience"] ?? "RetailPointFrontend";
            var expiryHours = int.Parse(_configuration["Jwt:ExpiryHours"] ?? "8");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, staff.StaffId.ToString()),
                new Claim(ClaimTypes.Name, staff.Username),
                new Claim("fullName", staff.FullName),
                new Claim("roleId", staff.RoleId.ToString()),
                new Claim("roleName", staff.Role?.RoleName ?? ""),
                new Claim("staffId", staff.StaffId.ToString()),
            };

            // Thêm storeId nếu có
            if (staff.StoreId.HasValue)
                claims.Add(new Claim("storeId", staff.StoreId.Value.ToString()));

            // Thêm permissions vào token
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expiryHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            var secretKey = _configuration["Jwt:SecretKey"];
            if (string.IsNullOrEmpty(secretKey)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
