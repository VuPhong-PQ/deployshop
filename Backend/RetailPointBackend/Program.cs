using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using OfficeOpenXml;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình server listen trên IP production - chỉ HTTP
// NOTE: Don't force a fixed URL when running under IIS (in-process or out-of-process).
// When the app is hosted by IIS/ANCM, ANCM controls the address/port. For local
// self-host scenarios (not under IIS) we keep the explicit binding.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_IIS_PHYSICAL_PATH")))
{
    builder.WebHost.UseUrls("http://localhost:5273");
}

// Cấu hình encoding UTF-8
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Thêm cấu hình CORS - chỉ cho phép các origin hợp lệ
var allowedOrigins = builder.Configuration["AllowedOrigins"]?.Split(',') 
    ?? new[] { "http://101.53.9.76", "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add sessions support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Cấu hình để trả về UTF-8
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(options =>
    {
        // Cấu hình để sử dụng camelCase cho JSON serialization
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        // Xử lý reference loops
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddOpenApi();

// Cấu hình JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey not configured in appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Đăng ký JwtService
builder.Services.AddSingleton<IJwtService, JwtService>();

// Add DbContext for EF Core - chỉ sử dụng AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký NotificationService
builder.Services.AddScoped<INotificationService, NotificationService>();

// Đăng ký SeedDataService
builder.Services.AddScoped<SeedDataService>();

// Đăng ký PermissionService
builder.Services.AddScoped<IPermissionService, PermissionService>();

// Đăng ký FixEncodingService
builder.Services.AddScoped<FixEncodingService>();

// Đăng ký EInvoiceService
// Sử dụng VNPT service cho production
builder.Services.AddHttpClient<IEInvoiceService, VNPTEInvoiceService>();
// Hoặc sử dụng MockEInvoiceService cho testing
// builder.Services.AddScoped<IEInvoiceService, MockEInvoiceService>();

// Đăng ký BackupScheduleService
builder.Services.AddScoped<IBackupScheduleService, BackupScheduleService>();

// Đăng ký DiscountService
builder.Services.AddScoped<IDiscountService, DiscountService>();

// Đăng ký ImageSearchService
builder.Services.AddHttpClient<IImageSearchService, ImageSearchService>();

// Đăng ký LoyaltyService
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();

// Đăng ký Background Service cho backup tự động
// Register BackupScheduleBackgroundService as singleton so controllers can access it
builder.Services.AddSingleton<RetailPointBackend.BackgroundServices.BackupScheduleBackgroundService>();
builder.Services.AddHostedService<RetailPointBackend.BackgroundServices.BackupScheduleBackgroundService>(provider => 
    provider.GetRequiredService<RetailPointBackend.BackgroundServices.BackupScheduleBackgroundService>());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Add global exception handling
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        
        var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        if (error != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(error.Error, "Unhandled exception occurred: {Message}", error.Error.Message);
            
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                error = "Internal server error",
                message = app.Environment.IsDevelopment() ? error.Error.Message : "An error occurred",
                details = app.Environment.IsDevelopment() ? error.Error.StackTrace : null
            }));
        }
    });
});

// Bật CORS đầu tiên
app.UseCors();

// Bật phục vụ file tĩnh (ảnh upload)
app.UseStaticFiles();

// JWT Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Thêm session middleware
app.UseSession();

// WebSocket support
app.UseWebSockets();
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        while (!result.CloseStatus.HasValue)
        {
            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }
        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});

// Kích hoạt các route controller (api/...)
app.MapControllers();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// Seed initial data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var seedService = scope.ServiceProvider.GetRequiredService<SeedDataService>();
        await seedService.SeedAsync();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Seed data failed: {ex.Message}");
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
