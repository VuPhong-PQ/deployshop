using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SimpleBackupController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SimpleBackupController> _logger;

        public SimpleBackupController(IConfiguration configuration, ILogger<SimpleBackupController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Simple backup controller working", timestamp = DateTime.Now });
        }

        [HttpPost("backup-download")]
        public async Task<IActionResult> BackupAndDownload()
        {
            try
            {
                _logger.LogInformation("Starting simple backup-and-download operation");
                
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string is null or empty");
                    return BadRequest(new { message = "Connection string không được cấu hình" });
                }
                
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                
                _logger.LogInformation($"Database name: {databaseName}");

                // Tạo tên file backup với timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_simple_backup_{timestamp}.bak";
                
                // Lưu tạm vào temp folder
                var tempBackupPath = Path.Combine(Path.GetTempPath(), backupFileName);
                _logger.LogInformation($"Temp backup path: {tempBackupPath}");

                // Câu lệnh backup SQL
                var backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{tempBackupPath}' 
                    WITH FORMAT, INIT, NAME = 'Simple Full Backup of {databaseName}', 
                    SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                _logger.LogInformation("Executing backup command");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        command.CommandTimeout = 300; // 5 phút timeout
                        await command.ExecuteNonQueryAsync();
                    }
                }
                _logger.LogInformation("Backup command completed");

                // Kiểm tra file có tồn tại không
                if (!System.IO.File.Exists(tempBackupPath))
                {
                    _logger.LogError($"Backup file not found at: {tempBackupPath}");
                    return StatusCode(500, new { message = "File backup không được tạo" });
                }

                // Lấy thông tin file backup
                var fileInfo = new FileInfo(tempBackupPath);
                var fileSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                _logger.LogInformation($"Backup file size: {fileSizeMB} MB");

                // Đọc file để download
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempBackupPath);
                _logger.LogInformation($"File read successfully, {fileBytes.Length} bytes");
                
                // Xóa file tạm sau khi đọc
                try
                {
                    System.IO.File.Delete(tempBackupPath);
                    _logger.LogInformation("Temp file deleted");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể xóa file tạm: {TempPath}", tempBackupPath);
                }

                // Thiết lập Content-Disposition header cho download
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{backupFileName}\"";
                
                return File(fileBytes, "application/octet-stream", backupFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi backup và download database");
                return StatusCode(500, new { message = "Lỗi khi backup và download database", error = ex.Message, details = ex.ToString() });
            }
        }
    }
}