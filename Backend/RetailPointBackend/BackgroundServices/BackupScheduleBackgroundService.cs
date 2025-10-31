using RetailPointBackend.Services;
using RetailPointBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace RetailPointBackend.BackgroundServices
{
    public class BackupScheduleBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BackupScheduleBackgroundService> _logger;
        private TimeSpan _scheduledTime = new TimeSpan(13, 0, 0); // Default: 1:00 PM (13:00)
        private bool _isEnabled = true;
        private readonly object _configLock = new object();

        public BackupScheduleBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<BackupScheduleBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        // Method to update schedule dynamically
        public void UpdateSchedule(TimeSpan newTime, bool isEnabled)
        {
            lock (_configLock)
            {
                _scheduledTime = newTime;
                _isEnabled = isEnabled;
                _logger.LogInformation("Backup schedule updated: Time={Time}, Enabled={Enabled}", 
                    newTime, isEnabled);
            }
        }

        // Public method to execute backup manually
        public async Task ExecuteBackupAsync()
        {
            await ExecuteBackupJobAsync();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Service Backup Schedule đã khởi động");

            // Load initial settings from database
            await LoadBackupSettingsAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Reload settings periodically to get updates
                    await LoadBackupSettingsAsync();

                    if (!_isEnabled)
                    {
                        _logger.LogInformation("Backup is disabled. Waiting 1 hour before checking again.");
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                        continue;
                    }

                    var now = DateTime.Now;
                    var nextRun = GetNextRunTime(now);
                    var delay = nextRun - now;

                    _logger.LogInformation("Lần backup tiếp theo: {NextRun} (sau {Delay})", 
                        nextRun.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                    // Chờ đến thời gian backup hoặc cancellation
                    await Task.Delay(delay, stoppingToken);

                    if (!stoppingToken.IsCancellationRequested && _isEnabled)
                    {
                        await ExecuteBackupJobAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Service đang được dừng
                    _logger.LogInformation("Background Service Backup Schedule đã được dừng");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong Background Service Backup Schedule");
                    
                    // Chờ 1 giờ trước khi thử lại nếu có lỗi
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        private async Task LoadBackupSettingsAsync()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var settings = await context.BackupSettings.FirstOrDefaultAsync();
                if (settings != null)
                {
                    lock (_configLock)
                    {
                        _scheduledTime = settings.BackupTime;
                        _isEnabled = settings.IsEnabled;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading backup settings from database");
            }
        }

        private DateTime GetNextRunTime(DateTime now)
        {
            TimeSpan scheduledTime;
            lock (_configLock)
            {
                scheduledTime = _scheduledTime;
            }

            var today = now.Date;
            var todayScheduledTime = today.Add(scheduledTime);

            // Nếu chưa đến giờ hôm nay thì chạy hôm nay, ngược lại chạy ngày mai
            if (now < todayScheduledTime)
            {
                return todayScheduledTime;
            }
            else
            {
                return today.AddDays(1).Add(_scheduledTime);
            }
        }

        private async Task ExecuteBackupJobAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    var backupService = scope.ServiceProvider.GetRequiredService<IBackupScheduleService>();
                    await backupService.ExecuteBackupAsync();
                    
                    _logger.LogInformation("Backup tự động hoàn thành thành công vào {Time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi thực hiện backup tự động vào {Time}", DateTime.Now);
                }
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Đang dừng Background Service Backup Schedule...");
            await base.StopAsync(stoppingToken);
        }
    }
}