using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using OfficeOpenXml;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình encoding UTF-8
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Thêm cấu hình CORS cho phép từ frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173", 
            "http://localhost:5174", 
            "http://localhost:5175", 
            "http://101.53.9.76", 
            "http://101.53.9.76:80",
            "http://101.53.9.76:3000",
            "http://101.53.9.76:8080",
            "https://101.53.9.76",
            "https://101.53.9.76:443",
            "*" // Temporarily allow all origins for debugging
        )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Thêm dòng này!
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

// Add DbContext for EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký thêm RetailPointContext cho các controller cũ
builder.Services.AddDbContext<RetailPointContext>(options =>
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

// Kích hoạt các route controller (api/...)
app.MapControllers();



// Bật phục vụ file tĩnh (ảnh upload)
app.UseStaticFiles();
app.UseHttpsRedirection();
// Bật CORS trước khi map controller
app.UseCors();

// Thêm session middleware
app.UseSession();

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
