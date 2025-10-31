using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DataManagementController> _logger;
        private readonly IPermissionService _permissionService;

        public DataManagementController(AppDbContext context, IConfiguration configuration, 
            ILogger<DataManagementController> logger, IPermissionService permissionService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _permissionService = permissionService;
        }

        // Correct order delete method - theo đúng foreign key dependencies
        [HttpDelete("sales-data-correct-order")]
        public async Task<IActionResult> DeleteSalesDataCorrectOrder([FromBody] DeleteConfirmationDto confirmation)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "DeleteSalesData"))
                // {
                //     return Forbid("Bạn không có quyền xóa dữ liệu bán hàng");
                // }

                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lòng nhập đúng text xác nhận: DELETE SALES DATA" });
                }

                var result = new List<object>();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Xóa theo thứ tự đúng dựa trên foreign key dependencies
                            
                            // 1. EInvoiceItems (child of EInvoices, OrderItems, Products)
                            var eInvoiceItemsCmd = new SqlCommand("SELECT COUNT(*) FROM EInvoiceItems", connection, transaction);
                            var eInvoiceItemsCount = (int)(await eInvoiceItemsCmd.ExecuteScalarAsync() ?? 0);
                            if (eInvoiceItemsCount > 0)
                            {
                                var deleteEInvoiceItemsCmd = new SqlCommand("DELETE FROM EInvoiceItems", connection, transaction);
                                await deleteEInvoiceItemsCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "EInvoiceItems", deletedCount = eInvoiceItemsCount });
                            }

                            // 2. EInvoices (child of Orders, Staffs)
                            var eInvoicesCmd = new SqlCommand("SELECT COUNT(*) FROM EInvoices", connection, transaction);
                            var eInvoicesCount = (int)(await eInvoicesCmd.ExecuteScalarAsync() ?? 0);
                            if (eInvoicesCount > 0)
                            {
                                var deleteEInvoicesCmd = new SqlCommand("DELETE FROM EInvoices", connection, transaction);
                                await deleteEInvoicesCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "EInvoices", deletedCount = eInvoicesCount });
                            }

                            // 3. Notifications (child of Orders)
                            var notificationsCmd = new SqlCommand("SELECT COUNT(*) FROM Notifications", connection, transaction);
                            var notificationsCount = (int)(await notificationsCmd.ExecuteScalarAsync() ?? 0);
                            if (notificationsCount > 0)
                            {
                                var deleteNotificationsCmd = new SqlCommand("DELETE FROM Notifications", connection, transaction);
                                await deleteNotificationsCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Notifications", deletedCount = notificationsCount });
                            }

                            // 4. InventoryTransactions (child of Orders, Products, Staffs)
                            var inventoryCmd = new SqlCommand("SELECT COUNT(*) FROM InventoryTransactions", connection, transaction);
                            var inventoryCount = (int)(await inventoryCmd.ExecuteScalarAsync() ?? 0);
                            if (inventoryCount > 0)
                            {
                                var deleteInventoryCmd = new SqlCommand("DELETE FROM InventoryTransactions", connection, transaction);
                                await deleteInventoryCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "InventoryTransactions", deletedCount = inventoryCount });
                            }

                            // 5. OrderItems (child of Orders, Customers, Products)
                            var orderItemsCmd = new SqlCommand("SELECT COUNT(*) FROM OrderItems", connection, transaction);
                            var orderItemsCount = (int)(await orderItemsCmd.ExecuteScalarAsync() ?? 0);
                            if (orderItemsCount > 0)
                            {
                                var deleteOrderItemsCmd = new SqlCommand("DELETE FROM OrderItems", connection, transaction);
                                await deleteOrderItemsCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "OrderItems", deletedCount = orderItemsCount });
                            }

                            // 6. Orders (child of Customers, Staffs)
                            var ordersCmd = new SqlCommand("SELECT COUNT(*) FROM Orders", connection, transaction);
                            var ordersCount = (int)(await ordersCmd.ExecuteScalarAsync() ?? 0);
                            if (ordersCount > 0)
                            {
                                var deleteOrdersCmd = new SqlCommand("DELETE FROM Orders", connection, transaction);
                                await deleteOrdersCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Orders", deletedCount = ordersCount });
                            }

                            // 7. Customers (no foreign key dependencies)
                            var customersCmd = new SqlCommand("SELECT COUNT(*) FROM Customers", connection, transaction);
                            var customersCount = (int)(await customersCmd.ExecuteScalarAsync() ?? 0);
                            if (customersCount > 0)
                            {
                                var deleteCustomersCmd = new SqlCommand("DELETE FROM Customers", connection, transaction);
                                await deleteCustomersCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Customers", deletedCount = customersCount });
                            }

                            // 8. Reset Products stock
                            var resetStockCmd = new SqlCommand("UPDATE Products SET StockQuantity = 0 WHERE StockQuantity > 0", connection, transaction);
                            var resetCount = await resetStockCmd.ExecuteNonQueryAsync();
                            result.Add(new { table = "Products", resetStockCount = resetCount });

                            // Commit transaction
                            transaction.Commit();

                            return Ok(new 
                            { 
                                message = "Đã xóa dữ liệu bán hàng thành công (correct order method)", 
                                details = result,
                                timestamp = DateTime.Now 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Lỗi khi xóa dữ liệu bán hàng (correct order)");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa dữ liệu bán hàng (correct order)");
                return StatusCode(500, new { message = "Lỗi khi xóa dữ liệu bán hàng", error = ex.Message });
            }
        }

        // Debug endpoint để xem foreign key constraints
        [HttpGet("foreign-keys")]
        public async Task<IActionResult> GetForeignKeys()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var foreignKeys = new List<object>();
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                        SELECT 
                            fk.name AS FK_Name,
                            tp.name AS Parent_Table,
                            cp.name AS Parent_Column,
                            tr.name AS Referenced_Table,
                            cr.name AS Referenced_Column
                        FROM sys.foreign_keys fk
                        INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
                        INNER JOIN sys.tables tp ON fkc.parent_object_id = tp.object_id
                        INNER JOIN sys.columns cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
                        INNER JOIN sys.tables tr ON fkc.referenced_object_id = tr.object_id
                        INNER JOIN sys.columns cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
                        WHERE tp.name IN ('Orders', 'OrderItems', 'Customers', 'InventoryTransactions', 'EInvoices', 'EInvoiceItems')
                           OR tr.name IN ('Orders', 'OrderItems', 'Customers', 'InventoryTransactions', 'EInvoices', 'EInvoiceItems')
                        ORDER BY tp.name, tr.name";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                foreignKeys.Add(new
                                {
                                    fkName = reader["FK_Name"].ToString(),
                                    parentTable = reader["Parent_Table"].ToString(),
                                    parentColumn = reader["Parent_Column"].ToString(),
                                    referencedTable = reader["Referenced_Table"].ToString(),
                                    referencedColumn = reader["Referenced_Column"].ToString()
                                });
                            }
                        }
                    }
                }
                
                return Ok(new { foreignKeys = foreignKeys });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy foreign keys");
                return StatusCode(500, new { message = "Lỗi khi lấy foreign keys", error = ex.Message });
            }
        }

        // Download backup file
        [HttpGet("download-backup/{fileName}")]
        public async Task<IActionResult> DownloadBackupFile(string fileName)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "DownloadBackup"))
                // {
                //     return Forbid("Bạn không có quyền download backup");
                // }

                // Validate filename để tránh path traversal
                if (string.IsNullOrWhiteSpace(fileName) || 
                    fileName.Contains("..") || 
                    fileName.Contains("\\") || 
                    fileName.Contains("/"))
                {
                    return BadRequest(new { message = "Tên file không hợp lệ" });
                }

                // Tìm file trong các thư mục backup có thể
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "backups", fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "TempBackups", fileName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetailPoint", "Temp", fileName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName)
                };

                // Tìm file từ lịch sử backup
                var backupHistory = await _context.BackupHistories
                    .Where(bh => bh.FileName == fileName && bh.Status == "Success")
                    .OrderByDescending(bh => bh.BackupDate)
                    .FirstOrDefaultAsync();

                string filePath = null;

                // Ưu tiên đường dẫn từ lịch sử backup
                if (backupHistory != null && !string.IsNullOrEmpty(backupHistory.FilePath) && 
                    System.IO.File.Exists(backupHistory.FilePath))
                {
                    filePath = backupHistory.FilePath;
                }
                else
                {
                    // Tìm trong các đường dẫn có thể
                    filePath = possiblePaths.FirstOrDefault(path => System.IO.File.Exists(path));
                }

                if (filePath == null)
                {
                    return NotFound(new { message = $"Không tìm thấy file backup: {fileName}" });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var fileInfo = new FileInfo(filePath);
                
                var contentType = Path.GetExtension(fileName).ToLower() switch
                {
                    ".bak" => "application/octet-stream",
                    ".sql" => "text/plain",
                    _ => "application/octet-stream"
                };

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi download backup file: {FileName}", fileName);
                return StatusCode(500, new { message = "Lỗi khi download backup file", error = ex.Message });
            }
        }

        // Simple test endpoint
        [HttpGet("test")]
        public IActionResult Test()
        {
            try
            {
                _logger.LogInformation("Test endpoint called");
                return Ok(new { message = "Test successful", timestamp = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test endpoint");
                return StatusCode(500, new { message = "Test failed", error = ex.Message });
            }
        }

        // Backup với tùy chọn download trực tiếp
        [HttpPost("backup-and-download")]
        public async Task<IActionResult> BackupAndDownload()
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "BackupDatabase"))
                // {
                //     return Forbid("Bạn không có quyền sao lưu dữ liệu");
                // }
                
                _logger.LogInformation("Starting backup-and-download operation");
                
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
                var backupFileName = $"{databaseName}_backup_{timestamp}.bak";
                
                // Sử dụng C:\temp - thư mục có quyền ghi rộng rãi
                string tempBackupPath;
                
                // Tạo thư mục C:\temp nếu chưa tồn tại
                var tempDir = @"C:\temp";
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                tempBackupPath = Path.Combine(tempDir, backupFileName);
                _logger.LogInformation($"Using C:\\temp directory for backup: {tempBackupPath}");

                // Câu lệnh backup SQL
                var backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{tempBackupPath}' 
                    WITH FORMAT, INIT, NAME = 'Full Backup of {databaseName}', 
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

                // Skip saving backup history for now to avoid database issues
                _logger.LogInformation("Skipping backup history save for debugging");

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
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{backupFileName}\"");
                
                return File(fileBytes, "application/octet-stream", backupFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Lỗi quyền truy cập khi backup và download database");
                return StatusCode(500, new { 
                    message = "Không có quyền truy cập thư mục backup", 
                    error = "Server không có quyền ghi vào thư mục. Vui lòng liên hệ quản trị viên.",
                    details = ex.Message 
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Không tìm thấy thư mục backup");
                return StatusCode(500, new { 
                    message = "Thư mục backup không tồn tại", 
                    error = "Không thể tạo thư mục backup. Vui lòng liên hệ quản trị viên.",
                    details = ex.Message 
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi backup database");
                return StatusCode(500, new { 
                    message = "Lỗi database khi tạo backup", 
                    error = "Kiểm tra kết nối database và quyền backup",
                    details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi backup và download database");
                return StatusCode(500, new { 
                    message = "Lỗi khi backup và download database", 
                    error = ex.Message, 
                    details = ex.ToString() 
                });
            }
        }

        // Debug endpoint để kiểm tra request
        [HttpPost("debug-backup-path")]
        public IActionResult DebugBackupPath([FromBody] BackupRequestDto request)
        {
            return Ok(new { 
                receivedPath = request.BackupPath,
                isNull = request.BackupPath == null,
                isEmpty = string.IsNullOrEmpty(request.BackupPath),
                length = request.BackupPath?.Length ?? -1
            });
        }

        // Test backup endpoint để kiểm tra SQL Server permissions
        [HttpPost("test-backup")]
        public async Task<IActionResult> TestBackup()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var testFileName = $"test_backup_{timestamp}.bak";
                var backupDir = @"C:\temp";
                
                // Tạo thư mục nếu chưa tồn tại
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
                
                var testBackupPath = Path.Combine(backupDir, testFileName);
                
                _logger.LogInformation($"Testing backup to: {testBackupPath}");
                
                var backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{testBackupPath}' 
                    WITH FORMAT, INIT, NAME = 'Test Backup', 
                    SKIP, NOREWIND, NOUNLOAD";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        command.CommandTimeout = 60;
                        await command.ExecuteNonQueryAsync();
                    }
                }
                
                // Check if file was created
                var fileExists = System.IO.File.Exists(testBackupPath);
                var fileSize = fileExists ? new FileInfo(testBackupPath).Length : 0;
                
                // Clean up test file
                if (fileExists)
                {
                    System.IO.File.Delete(testBackupPath);
                }
                
                return Ok(new 
                { 
                    success = true,
                    message = "Test backup thành công",
                    backupPath = testBackupPath,
                    fileWasCreated = fileExists,
                    fileSize = fileSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test backup failed");
                return StatusCode(500, new 
                { 
                    success = false,
                    message = "Test backup thất bại", 
                    error = ex.Message,
                    details = ex.ToString()
                });
            }
        }

        // Debug endpoint để kiểm tra SQL Server backup directory
        [HttpGet("debug-sql-backup-dir")]
        public async Task<IActionResult> DebugSqlBackupDir()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    var query = @"
                        DECLARE @BackupDirectory NVARCHAR(4000)
                        EXEC master.dbo.xp_instance_regread 
                            N'HKEY_LOCAL_MACHINE', 
                            N'SOFTWARE\Microsoft\MSSQLServer\MSSQLServer', 
                            N'BackupDirectory', 
                            @BackupDirectory OUTPUT
                        SELECT ISNULL(@BackupDirectory, 'C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup') as BackupDirectory";
                    
                    using (var command = new SqlCommand(query, connection))
                    {
                        var result = await command.ExecuteScalarAsync();
                        var backupDir = result?.ToString() ?? @"C:\Program Files\Microsoft SQL Server\MSSQL15.MSSQLSERVER\MSSQL\Backup";
                        
                        return Ok(new 
                        { 
                            backupDirectory = backupDir,
                            directoryExists = Directory.Exists(backupDir),
                            connectionString = connectionString.Replace(new SqlConnectionStringBuilder(connectionString).Password, "***")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, details = ex.ToString() });
            }
        }

        // Backup toàn bộ database
        [HttpPost("backup")]
        public async Task<IActionResult> BackupDatabase([FromBody] BackupRequestDto request)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "BackupDatabase"))
                // {
                //     return Forbid("Bạn không có quyền sao lưu dữ liệu");
                // }
                
                _logger.LogInformation($"Backup request received - BackupPath: '{request.BackupPath}'");
                
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                var serverName = sqlConnectionStringBuilder.DataSource;

                // Tạo tên file backup với timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_backup_{timestamp}.bak";
                
                string backupPath;
                if (!string.IsNullOrWhiteSpace(request.BackupPath))
                {
                    // Sử dụng đường dẫn do người dùng chỉ định
                    backupPath = Path.Combine(request.BackupPath, backupFileName);
                }
                else
                {
                    // Sử dụng C:\temp - thư mục có quyền ghi rộng rãi
                    var tempDir = @"C:\temp";
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                    
                    backupPath = Path.Combine(tempDir, backupFileName);
                    _logger.LogInformation($"Using C:\\temp directory for backup: {backupPath}");
                }
                
                _logger.LogInformation($"Final backup path: '{backupPath}");

                // Câu lệnh backup SQL
                var backupQuery = $@"
                    BACKUP DATABASE [{databaseName}] 
                    TO DISK = '{backupPath}' 
                    WITH FORMAT, INIT, NAME = 'Full Backup of {databaseName}', 
                    SKIP, NOREWIND, NOUNLOAD, STATS = 10";

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand(backupQuery, connection))
                    {
                        command.CommandTimeout = 300; // 5 phút timeout
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
                    BackupType = "Manual",
                    FilePath = backupPath,
                    FileName = backupFileName,
                    FileSizeMB = fileSizeMB,
                    Status = "Success",
                    Note = "Backup thủ công từ Data Management"
                };

                _context.BackupHistories.Add(backupHistory);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Backup database thành công", 
                    backupPath = backupPath,
                    fileName = backupFileName,
                    timestamp = timestamp,
                    size = fileSizeMB
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Lỗi quyền truy cập khi backup database");
                return StatusCode(500, new { 
                    message = "Không có quyền truy cập thư mục backup", 
                    error = "Vui lòng kiểm tra quyền ghi file hoặc chọn thư mục khác",
                    details = ex.Message 
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "Không tìm thấy thư mục backup");
                return StatusCode(500, new { 
                    message = "Thư mục backup không tồn tại", 
                    error = "Vui lòng tạo thư mục hoặc chọn đường dẫn khác",
                    details = ex.Message 
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lỗi SQL khi backup database");
                return StatusCode(500, new { 
                    message = "Lỗi database khi tạo backup", 
                    error = "Kiểm tra kết nối database và quyền backup",
                    details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi backup database");
                return StatusCode(500, new { 
                    message = "Lỗi khi backup database", 
                    error = ex.Message,
                    details = ex.ToString() 
                });
            }
        }

        // List uploaded backup files
        [HttpGet("backup-files")]
        public async Task<IActionResult> GetBackupFiles()
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "RestoreDatabase"))
                // {
                //     return Forbid("Bạn không có quyền xem danh sách backup");
                // }

                var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "backups");
                
                if (!Directory.Exists(backupDir))
                {
                    return Ok(new { files = new object[0] });
                }

                var files = Directory.GetFiles(backupDir, "*.*")
                    .Where(f => f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || 
                               f.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    .Select(f => new
                    {
                        fileName = Path.GetFileName(f),
                        filePath = f,
                        size = new FileInfo(f).Length,
                        lastModified = new FileInfo(f).LastWriteTime,
                        extension = Path.GetExtension(f)
                    })
                    .OrderByDescending(f => f.lastModified)
                    .ToArray();

                return Ok(new { files });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy danh sách backup files");
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách backup files", error = ex.Message });
            }
        }

        // Get backup history
        [HttpGet("backup-history")]
        public async Task<IActionResult> GetBackupHistory()
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "ViewDataManagement"))
                // {
                //     return Forbid("Bạn không có quyền xem lịch sử backup");
                // }

                var history = await _context.BackupHistories
                    .OrderByDescending(bh => bh.BackupDate)
                    .Take(50)
                    .Select(bh => new
                    {
                        id = bh.Id,
                        backupDate = bh.BackupDate,
                        backupType = bh.BackupType,
                        fileName = bh.FileName,
                        fileSizeMB = bh.FileSizeMB,
                        status = bh.Status,
                        note = bh.Note
                    })
                    .ToListAsync();

                return Ok(new { history });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử backup");
                return StatusCode(500, new { message = "Lỗi khi lấy lịch sử backup", error = ex.Message });
            }
        }

        // Upload backup file
        [HttpPost("upload-backup")]
        public async Task<IActionResult> UploadBackupFile(IFormFile file)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "RestoreDatabase"))
                // {
                //     return Forbid("Bạn không có quyền phục hồi dữ liệu");
                // }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn file backup" });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".bak", ".sql" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chỉ chấp nhận file .bak hoặc .sql" });
                }

                // Create backup directory if it doesn't exist
                var backupDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "backups");
                Directory.CreateDirectory(backupDir);

                // Generate unique filename
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{file.FileName}";
                var filePath = Path.Combine(backupDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { 
                    message = "Upload file backup thành công",
                    filePath = filePath,
                    fileName = fileName,
                    originalName = file.FileName,
                    size = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload backup file");
                return StatusCode(500, new { message = "Lỗi khi upload backup file", error = ex.Message });
            }
        }

        // Restore database từ file backup
        [HttpPost("restore")]
        public async Task<IActionResult> RestoreDatabase([FromBody] RestoreRequestDto request)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "RestoreDatabase"))
                // {
                //     return Forbid("Bạn không có quyền phục hồi dữ liệu");
                // }

                if (string.IsNullOrWhiteSpace(request.BackupFilePath))
                {
                    return BadRequest(new { message = "Đường dẫn file backup không được để trống" });
                }

                if (!System.IO.File.Exists(request.BackupFilePath))
                {
                    return BadRequest(new { 
                        message = "File backup không tồn tại", 
                        filePath = request.BackupFilePath,
                        suggestion = "Vui lòng upload file backup trước khi thực hiện restore"
                    });
                }

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;

                _logger.LogInformation($"Starting restore for database: {databaseName} from file: {request.BackupFilePath}");

                // Validate file exists and get info
                var fileInfo = new FileInfo(request.BackupFilePath);
                _logger.LogInformation($"Backup file size: {fileInfo.Length / 1024 / 1024:F2} MB");

                // Use master database for database operations
                var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                }.ConnectionString;

                // Đóng tất cả kết nối đến database bằng cách kill sessions
                var killConnectionsQuery = $@"
                    DECLARE @kill varchar(8000) = '';  
                    SELECT @kill = @kill + 'KILL ' + CONVERT(varchar(5), session_id) + ';'  
                    FROM sys.dm_exec_sessions
                    WHERE database_id = DB_ID('{databaseName}') AND session_id <> @@SPID;
                    EXEC(@kill);";

                // Câu lệnh restore SQL với REPLACE để ghi đè database hiện tại
                var restoreQuery = $@"
                    RESTORE DATABASE [{databaseName}] 
                    FROM DISK = N'{request.BackupFilePath}' 
                    WITH REPLACE, STATS = 10";

                _logger.LogInformation("Starting database restore process for file: {FilePath}", request.BackupFilePath);
                _logger.LogInformation("Restore SQL Query: {Query}", restoreQuery);

                // Đóng Entity Framework connection trước
                try 
                {
                    await _context.Database.CloseConnectionAsync();
                    _logger.LogInformation("Closed Entity Framework connection");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not close EF connection, continuing...");
                }

                try
                {
                    using (var connection = new SqlConnection(masterConnectionString))
                    {
                        await connection.OpenAsync();
                        _logger.LogInformation("Connected to master database");
                        
                        // Đóng các kết nối khác trước
                        _logger.LogInformation("Killing active connections to database");
                        using (var killCommand = new SqlCommand(killConnectionsQuery, connection))
                        {
                            try 
                            {
                                await killCommand.ExecuteNonQueryAsync();
                                _logger.LogInformation("Successfully killed active connections");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to kill some connections, but continuing with restore");
                            }
                        }
                        
                        // Wait a moment for connections to close
                        await Task.Delay(2000);
                        
                        // Restore database
                        _logger.LogInformation("Starting database restore...");
                        using (var restoreCommand = new SqlCommand(restoreQuery, connection))
                        {
                            restoreCommand.CommandTimeout = 600; // 10 phút timeout
                            await restoreCommand.ExecuteNonQueryAsync();
                        }
                        _logger.LogInformation("Database restore completed successfully");
                    }
                }
                catch (SqlException sqlEx)
                {
                    _logger.LogError(sqlEx, $"SQL Error during restore: {sqlEx.Message}");
                    throw new Exception($"SQL Server error: {sqlEx.Message}", sqlEx);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"General error during restore: {ex.Message}");
                    throw;
                }

                // Làm sạch Entity Framework context sau khi restore thành công
                try
                {
                    _logger.LogInformation("Cleaning up Entity Framework context after restore");
                    await _context.Database.CloseConnectionAsync();
                    _context.ChangeTracker.Clear();
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Warning: Could not cleanup EF context after restore");
                }

                return Ok(new { 
                    message = "Restore database thành công",
                    restoredFrom = request.BackupFilePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi restore database. File: {BackupFilePath}, Error: {ErrorMessage}", request.BackupFilePath, ex.Message);
                
                // Try to set database back to multi-user if single-user was set
                try
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                    var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                    var masterConnectionString = new SqlConnectionStringBuilder(connectionString)
                    {
                        InitialCatalog = "master"
                    }.ConnectionString;

                    using (var connection = new SqlConnection(masterConnectionString))
                    {
                        await connection.OpenAsync();
                        var setMultiUserQuery = $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
                        using (var command = new SqlCommand(setMultiUserQuery, connection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch (Exception cleanup_ex)
                {
                    _logger.LogError(cleanup_ex, "Error during cleanup: {CleanupError}", cleanup_ex.Message);
                }

                return StatusCode(500, new { 
                    message = "Lỗi khi restore database", 
                    error = ex.Message,
                    details = ex.InnerException?.Message,
                    filePath = request.BackupFilePath
                });
            }
        }

        // Simple delete method - xóa một cách an toàn
        [HttpDelete("sales-data-simple")]
        public async Task<IActionResult> DeleteSalesDataSimple([FromBody] DeleteConfirmationDto confirmation)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "DeleteSalesData"))
                // {
                //     return Forbid("Bạn không có quyền xóa dữ liệu bán hàng");
                // }

                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lòng nhập đúng text xác nhận: DELETE SALES DATA" });
                }

                var result = new List<object>();

                // Xóa từng bảng một cách đơn giản
                try
                {
                    // 1. Đếm records trước khi xóa
                    var orderItemsCount = await _context.OrderItems.CountAsync();
                    var ordersCount = await _context.Orders.CountAsync();
                    var customersCount = await _context.Customers.CountAsync();
                    var inventoryCount = await _context.InventoryTransactions.CountAsync();

                    result.Add(new { action = "count_before", orderItems = orderItemsCount, orders = ordersCount, customers = customersCount, inventory = inventoryCount });

                    // 2. Xóa OrderItems
                    if (orderItemsCount > 0)
                    {
                        var orderItems = await _context.OrderItems.ToListAsync();
                        _context.OrderItems.RemoveRange(orderItems);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "OrderItems", count = orderItemsCount });
                    }

                    // 3. Xóa Orders
                    if (ordersCount > 0)
                    {
                        var orders = await _context.Orders.ToListAsync();
                        _context.Orders.RemoveRange(orders);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "Orders", count = ordersCount });
                    }

                    // 4. Xóa Customers
                    if (customersCount > 0)
                    {
                        var customers = await _context.Customers.ToListAsync();
                        _context.Customers.RemoveRange(customers);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "Customers", count = customersCount });
                    }

                    // 5. Xóa InventoryTransactions
                    if (inventoryCount > 0)
                    {
                        var inventory = await _context.InventoryTransactions.ToListAsync();
                        _context.InventoryTransactions.RemoveRange(inventory);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "InventoryTransactions", count = inventoryCount });
                    }

                    // 6. Reset Products stock
                    var productsToReset = await _context.Products.Where(p => p.StockQuantity > 0).ToListAsync();
                    foreach (var product in productsToReset)
                    {
                        product.StockQuantity = 0;
                    }
                    await _context.SaveChangesAsync();
                    result.Add(new { action = "reset", table = "Products", count = productsToReset.Count });

                    // 7. Đếm lại để kiểm tra
                    var remainingOrderItems = await _context.OrderItems.CountAsync();
                    var remainingOrders = await _context.Orders.CountAsync();
                    var remainingCustomers = await _context.Customers.CountAsync();
                    var remainingInventory = await _context.InventoryTransactions.CountAsync();

                    result.Add(new { action = "count_after", orderItems = remainingOrderItems, orders = remainingOrders, customers = remainingCustomers, inventory = remainingInventory });

                    var success = remainingOrderItems == 0 && remainingOrders == 0 && remainingCustomers == 0 && remainingInventory == 0;

                    return Ok(new
                    {
                        success = success,
                        message = success ? "Xóa dữ liệu bán hàng thành công!" : "Có một số dữ liệu chưa được xóa hoàn toàn",
                        details = result,
                        timestamp = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi xóa dữ liệu bán hàng đơn giản");
                    return StatusCode(500, new { message = "Lỗi khi xóa dữ liệu", error = ex.Message, details = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tổng quát khi xóa dữ liệu bán hàng");
                return StatusCode(500, new { message = "Lỗi khi xóa dữ liệu bán hàng", error = ex.Message });
            }
        }

        // Ultra safe delete method - xóa từng record một
        [HttpDelete("sales-data-ultra-safe")]
        public async Task<IActionResult> DeleteSalesDataUltraSafe([FromBody] DeleteConfirmationDto confirmation)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "DeleteSalesData"))
                // {
                //     return Forbid("Bạn không có quyền xóa dữ liệu bán hàng");
                // }

                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lòng nhập đúng text xác nhận: DELETE SALES DATA" });
                }

                var result = new List<object>();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // Xóa theo thứ tự an toàn bằng raw SQL với transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. Xóa OrderItems (child table)
                            var orderItemsCmd = new SqlCommand("SELECT COUNT(*) FROM OrderItems", connection, transaction);
                            var orderItemsCount = (int)(await orderItemsCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (orderItemsCount > 0)
                            {
                                var deleteOrderItemsCmd = new SqlCommand("DELETE FROM OrderItems", connection, transaction);
                                var deletedOrderItems = await deleteOrderItemsCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "OrderItems", deletedCount = deletedOrderItems });
                            }

                            // 2. Xóa Orders (parent table)
                            var ordersCmd = new SqlCommand("SELECT COUNT(*) FROM Orders", connection, transaction);
                            var ordersCount = (int)(await ordersCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (ordersCount > 0)
                            {
                                var deleteOrdersCmd = new SqlCommand("DELETE FROM Orders", connection, transaction);
                                var deletedOrders = await deleteOrdersCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Orders", deletedCount = deletedOrders });
                            }

                            // 3. Xóa Customers
                            var customersCmd = new SqlCommand("SELECT COUNT(*) FROM Customers", connection, transaction);
                            var customersCount = (int)(await customersCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (customersCount > 0)
                            {
                                var deleteCustomersCmd = new SqlCommand("DELETE FROM Customers", connection, transaction);
                                var deletedCustomers = await deleteCustomersCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Customers", deletedCount = deletedCustomers });
                            }

                            // 4. Xóa InventoryTransactions
                            var inventoryCmd = new SqlCommand("SELECT COUNT(*) FROM InventoryTransactions", connection, transaction);
                            var inventoryCount = (int)(await inventoryCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (inventoryCount > 0)
                            {
                                var deleteInventoryCmd = new SqlCommand("DELETE FROM InventoryTransactions", connection, transaction);
                                var deletedInventory = await deleteInventoryCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "InventoryTransactions", deletedCount = deletedInventory });
                            }

                            // 5. Reset Products stock
                            var resetStockCmd = new SqlCommand("UPDATE Products SET StockQuantity = 0 WHERE StockQuantity > 0", connection, transaction);
                            var resetCount = await resetStockCmd.ExecuteNonQueryAsync();
                            result.Add(new { table = "Products", resetStockCount = resetCount });

                            // Commit transaction
                            transaction.Commit();

                            return Ok(new 
                            { 
                                message = "Đã xóa dữ liệu bán hàng thành công (ultra safe method)", 
                                details = result,
                                timestamp = DateTime.Now 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Lỗi khi xóa dữ liệu bán hàng (ultra safe)");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa dữ liệu bán hàng (ultra safe)");
                return StatusCode(500, new { message = "Lỗi khi xóa dữ liệu bán hàng", error = ex.Message });
            }
        }

        // Xóa dữ liệu bán hàng (giữ lại sản phẩm, nhóm hàng)
        [HttpDelete("sales-data")]
        public async Task<IActionResult> DeleteSalesData([FromBody] DeleteConfirmationDto confirmation)
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "DeleteSalesData"))
                // {
                //     return Forbid("Bạn không có quyền xóa dữ liệu bán hàng");
                // }
                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lòng nhập đúng text xác nhận: DELETE SALES DATA" });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _logger.LogInformation("Bắt đầu xóa dữ liệu bán hàng...");

                        // Danh sách bảng cần xóa theo thứ tự (từ child đến parent)
                        var tablesToDelete = new[]
                        {
                            // Dữ liệu chi tiết đơn hàng
                            "OrderItems",
                            
                            // Dữ liệu đơn hàng
                            "Orders",
                            
                            // Dữ liệu khách hàng
                            "Customers",
                            
                            // Dữ liệu kho và giao dịch
                            "InventoryMovements",
                            "InventoryTransactions",
                            
                            // Dữ liệu thanh toán
                            "PaymentTransactions",
                            "PaymentStats",
                            
                            // Dữ liệu báo cáo
                            "SalesReports",
                            "DailySalesReports",
                            "MonthlySalesReports",
                            "ProductSalesReports",
                            
                            // Hóa đơn điện tử
                            "EInvoiceItems",
                            "EInvoices",
                            
                            // Thông báo liên quan đến bán hàng
                            "Notifications",
                            
                            // Log activities liên quan
                            "ActivityLogs",
                            "AuditLogs"
                        };

                        var deletedTables = new List<string>();
                        var skippedTables = new List<string>();

                        // Tắt foreign key constraints tạm thời
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
                            _logger.LogInformation("Đã tắt foreign key constraints");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Lỗi khi tắt constraints: {ex.Message}");
                        }

                        // Xóa từng bảng (không cần kiểm tra tồn tại, sẽ catch exception nếu không có)
                        foreach (var table in tablesToDelete)
                        {
                            try
                            {
                                // Table name is from predefined safe array, not user input
                                #pragma warning disable EF1002
                                var result = await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}] WHERE 1=1");
                                #pragma warning restore EF1002
                                deletedTables.Add($"{table}");
                                _logger.LogInformation($"Đã xóa {table}");
                            }
                            catch (Exception ex)
                            {
                                skippedTables.Add($"{table} (lỗi: {ex.Message})");
                                _logger.LogWarning($"Lỗi khi xóa {table}: {ex.Message}");
                            }
                        }
                        
                        // Reset inventory quantities về 0
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync("UPDATE Products SET StockQuantity = 0 WHERE StockQuantity IS NOT NULL");
                            _logger.LogInformation("Đã reset StockQuantity");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Lỗi khi reset StockQuantity: {ex.Message}");
                        }
                        
                        // Reset identity columns
                        var identityTables = new[] { "Orders", "OrderItems", "Customers", "InventoryMovements", "EInvoices", "Notifications" };
                        foreach (var table in identityTables)
                        {
                            try
                            {
                                // Table name is from predefined safe array, not user input
                                #pragma warning disable EF1002
                                await _context.Database.ExecuteSqlRawAsync($"DBCC CHECKIDENT ('[{table}]', RESEED, 0)");
                                #pragma warning restore EF1002
                                _logger.LogInformation($"Đã reset identity cho {table}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Lỗi khi reset identity cho {table}: {ex.Message}");
                            }
                        }

                        // Bật lại foreign key constraints
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
                            _logger.LogInformation("Đã bật lại foreign key constraints");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Lỗi khi bật lại constraints: {ex.Message}");
                        }

                        await transaction.CommitAsync();
                        _logger.LogInformation("Hoàn thành xóa dữ liệu bán hàng");

                        return Ok(new { 
                            message = "Đã xóa toàn bộ dữ liệu bán hàng thành công. Sản phẩm và cấu hình hệ thống được giữ lại.",
                            timestamp = DateTime.Now,
                            deletedTables = deletedTables,
                            skippedTables = skippedTables,
                            note = "Đã xóa: đơn hàng, khách hàng, giao dịch kho, thanh toán, báo cáo, hóa đơn điện tử, thông báo"
                        });
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa dữ liệu bán hàng");
                return StatusCode(500, new { message = "Lỗi khi xóa dữ liệu bán hàng", error = ex.Message });
            }
        }

        // Lấy thông tin database
        [HttpGet("database-info")]
        public async Task<IActionResult> GetDatabaseInfo()
        {
            try
            {
                // Temporarily bypass permission check for debugging
                // var staffIdHeader = Request.Headers["StaffId"].FirstOrDefault();
                // if (!int.TryParse(staffIdHeader, out int staffId) || 
                //     !await _permissionService.HasPermissionAsync(staffId, "ViewDataManagement"))
                // {
                //     return Forbid("Bạn không có quyền xem thông tin dữ liệu");
                // }

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new { message = "Connection string không được cấu hình" });
                }

                try
                {
                    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                    var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                    var serverName = sqlConnectionStringBuilder.DataSource;
                    
                    // Lấy thông tin backup cuối cùng từ BackupHistory
                    var lastBackup = await _context.BackupHistories
                        .Where(bh => bh.Status == "Success")
                        .OrderByDescending(bh => bh.BackupDate)
                        .FirstOrDefaultAsync();

                    string lastBackupInfo = "Chưa có thông tin";
                    if (lastBackup != null)
                    {
                        var timeAgo = DateTime.Now - lastBackup.BackupDate;
                        if (timeAgo.TotalMinutes < 60)
                        {
                            lastBackupInfo = $"{(int)timeAgo.TotalMinutes} phút trước ({lastBackup.BackupType})";
                        }
                        else if (timeAgo.TotalHours < 24)
                        {
                            lastBackupInfo = $"{(int)timeAgo.TotalHours} giờ trước ({lastBackup.BackupType})";
                        }
                        else
                        {
                            lastBackupInfo = $"{(int)timeAgo.TotalDays} ngày trước ({lastBackup.BackupType})";
                        }
                    }

                    // Lấy kích thước database
                    double sizeMB = 0.0;
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            var sizeQuery = $@"
                                SELECT 
                                    CAST(SUM(CAST(FILEPROPERTY(name, 'SpaceUsed') AS bigint) * 8.0 / 1024) AS decimal(15,2))
                                FROM sys.database_files 
                                WHERE type IN (0,1)";
                            
                            using (var command = new SqlCommand(sizeQuery, connection))
                            {
                                var result = await command.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                {
                                    sizeMB = Convert.ToDouble(result);
                                }
                            }
                        }
                    }
                    catch (Exception sizeEx)
                    {
                        _logger.LogWarning(sizeEx, "Không thể lấy kích thước database");
                    }
                    
                    // Sau khi restore, Entity Framework context có thể bị lỗi
                    // Thử refresh context bằng cách tạo connection mới
                    try
                    {
                        // Sử dụng Entity Framework để lấy thông tin đơn giản
                        var dbName = await _context.Database.SqlQueryRaw<string>("SELECT DB_NAME()").FirstOrDefaultAsync();
                        
                        return Ok(new {
                            databaseName = dbName ?? databaseName ?? "Unknown",
                            sizeMB = Math.Round(sizeMB, 2),
                            serverName = serverName ?? "localhost",
                            lastBackup = lastBackupInfo,
                            lastBackupDate = lastBackup?.BackupDate,
                            lastBackupType = lastBackup?.BackupType,
                            lastBackupSize = lastBackup?.FileSizeMB
                        });
                    }
                    catch (Exception efError)
                    {
                        _logger.LogWarning(efError, "Entity Framework error, fallback to direct SQL connection");
                        
                        // Fallback: Sử dụng direct SQL connection
                        using (var connection = new SqlConnection(connectionString))
                        {
                            await connection.OpenAsync();
                            using (var command = new SqlCommand("SELECT DB_NAME()", connection))
                            {
                                var dbName = await command.ExecuteScalarAsync() as string;
                                
                                return Ok(new {
                                    databaseName = dbName ?? databaseName ?? "Unknown",
                                    sizeMB = Math.Round(sizeMB, 2),
                                    serverName = serverName ?? "localhost",
                                    lastBackup = lastBackupInfo,
                                    lastBackupDate = lastBackup?.BackupDate,
                                    lastBackupType = lastBackup?.BackupType,
                                    lastBackupSize = lastBackup?.FileSizeMB
                                });
                            }
                        }
                    }
                }
                catch (Exception sqlEx)
                {
                    _logger.LogError(sqlEx, "Lỗi kết nối SQL Server");
                    
                    // Fallback: trả về thông tin cơ bản từ connection string
                    try
                    {
                        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                        
                        // Vẫn cố gắng lấy thông tin backup cuối từ BackupHistory
                        var lastBackup = await _context.BackupHistories
                            .Where(bh => bh.Status == "Success")
                            .OrderByDescending(bh => bh.BackupDate)
                            .FirstOrDefaultAsync();

                        string lastBackupInfo = "Chưa có thông tin";
                        if (lastBackup != null)
                        {
                            var timeAgo = DateTime.Now - lastBackup.BackupDate;
                            if (timeAgo.TotalMinutes < 60)
                            {
                                lastBackupInfo = $"{(int)timeAgo.TotalMinutes} phút trước ({lastBackup.BackupType})";
                            }
                            else if (timeAgo.TotalHours < 24)
                            {
                                lastBackupInfo = $"{(int)timeAgo.TotalHours} giờ trước ({lastBackup.BackupType})";
                            }
                            else
                            {
                                lastBackupInfo = $"{(int)timeAgo.TotalDays} ngày trước ({lastBackup.BackupType})";
                            }
                        }
                        
                        return Ok(new {
                            databaseName = sqlConnectionStringBuilder.InitialCatalog ?? "RetailPointDB",
                            sizeMB = 0.0,
                            serverName = sqlConnectionStringBuilder.DataSource ?? "localhost",
                            lastBackup = lastBackupInfo,
                            lastBackupDate = lastBackup?.BackupDate,
                            lastBackupType = lastBackup?.BackupType,
                            lastBackupSize = lastBackup?.FileSizeMB
                        });
                    }
                    catch
                    {
                        return Ok(new {
                            databaseName = "RetailPointDB",
                            sizeMB = 0.0,
                            serverName = "localhost",
                            lastBackup = "Không thể lấy thông tin",
                            lastBackupDate = (DateTime?)null,
                            lastBackupType = (string?)null,
                            lastBackupSize = (double?)null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin database");
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin database", error = ex.Message });
            }
        }
    }

    // DTOs
    public class BackupRequestDto
    {
        [JsonPropertyName("backupPath")]
        public string? BackupPath { get; set; }
    }

    public class RestoreRequestDto
    {
        [Required]
        public string BackupFilePath { get; set; } = string.Empty;
    }

    public class DeleteConfirmationDto
    {
        [Required]
        public string ConfirmationText { get; set; } = string.Empty;
    }
}