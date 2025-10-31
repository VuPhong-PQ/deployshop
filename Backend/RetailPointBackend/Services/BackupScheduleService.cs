using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RetailPointBackend.Models;
using System.Globalization;

namespace RetailPointBackend.Services
{
    public interface IBackupScheduleService
    {
        Task ExecuteBackupAsync();
        Task<List<BackupHistory>> GetBackupHistoryAsync();
    }

    public class BackupScheduleService : IBackupScheduleService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackupScheduleService> _logger;

        public BackupScheduleService(
            AppDbContext context, 
            IConfiguration configuration, 
            ILogger<BackupScheduleService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task ExecuteBackupAsync()
        {
            try
        {
                _logger.LogInformation("Bắt đầu backup tự động vào {Time}", DateTime.Now);

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new Exception("Connection string không được cấu hình");
                }

                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;

                // Tạo tên file backup với timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"RetailPoint_auto_backup_{timestamp}.bak";
                
                // Tạo thư mục backup nếu chưa tồn tại
                var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "Backups", "Auto");
                Directory.CreateDirectory(backupDir);
                
                var backupPath = Path.Combine(backupDir, backupFileName);

                // Thực hiện backup
                var backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{backupPath}' 
                    WITH FORMAT, INIT, COMPRESSION";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        command.CommandTimeout = 3600; // 1 hour timeout cho backup lớn
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Lấy thông tin file backup
                var fileInfo = new FileInfo(backupPath);
                var fileSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);

                // Lưu lịch sử backup vào database
                var backupHistory = new BackupHistory
                {
                    BackupDate = DateTime.Now,
                    BackupType = "Auto",
                    FilePath = backupPath,
                    FileName = backupFileName,
                    FileSizeMB = fileSizeMB,
                    Status = "Success",
                    Note = "Backup tự động hàng ngày"
                };

                _context.BackupHistories.Add(backupHistory);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Backup tự động hoàn thành thành công: {FileName}, Size: {Size}MB", 
                    backupFileName, fileSizeMB);

                // Dọn dẹp backup cũ (giữ lại 30 backup gần nhất)
                await CleanupOldBackupsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi thực hiện backup tự động");

                // Lưu lỗi vào lịch sử
                try
                {
                    var errorHistory = new BackupHistory
                    {
                        BackupDate = DateTime.Now,
                        BackupType = "Auto",
                        FilePath = "",
                        FileName = "",
                        FileSizeMB = 0,
                        Status = "Failed",
                        Note = $"Lỗi: {ex.Message}"
                    };

                    _context.BackupHistories.Add(errorHistory);
                    await _context.SaveChangesAsync();
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "Không thể lưu lỗi backup vào database");
                }

                throw;
            }
        }

        public async Task<List<BackupHistory>> GetBackupHistoryAsync()
        {
            return await _context.BackupHistories
                .OrderByDescending(bh => bh.BackupDate)
                .Take(50) // Lấy 50 backup gần nhất
                .ToListAsync();
        }

        private async Task CleanupOldBackupsAsync()
        {
            try
            {
                // Lấy danh sách backup tự động cũ (giữ lại 30 backup gần nhất)
                var oldBackups = await _context.BackupHistories
                    .Where(bh => bh.BackupType == "Auto" && bh.Status == "Success")
                    .OrderByDescending(bh => bh.BackupDate)
                    .Skip(30)
                    .ToListAsync();

                foreach (var oldBackup in oldBackups)
                {
                    try
                    {
                        // Xóa file vật lý
                        if (File.Exists(oldBackup.FilePath))
                        {
                            File.Delete(oldBackup.FilePath);
                        }

                        // Xóa record khỏi database
                        _context.BackupHistories.Remove(oldBackup);
                        
                        _logger.LogInformation("Đã xóa backup cũ: {FileName}", oldBackup.FileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể xóa backup cũ: {FileName}", oldBackup.FileName);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dọn dẹp backup cũ");
            }
        }
    }
}