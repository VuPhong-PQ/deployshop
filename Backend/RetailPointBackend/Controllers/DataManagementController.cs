using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using RetailPointBackend.Models;
using RetailPointBackend.Services;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RetailPointBackend.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
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

        // Correct order delete method - theo Ä‘Ãºng foreign key dependencies
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");
                // }

                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lÃ²ng nháº­p Ä‘Ãºng text xÃ¡c nháº­n: DELETE SALES DATA" });
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
                            // XÃ³a theo thá»© tá»± Ä‘Ãºng dá»±a trÃªn foreign key dependencies
                            
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
                                message = "ÄÃ£ xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng thÃ nh cÃ´ng (correct order method)", 
                                details = result,
                                timestamp = DateTime.Now 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng (correct order)");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng (correct order)");
                return StatusCode(500, new { message = "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng", error = ex.Message });
            }
        }

        // Debug endpoint Ä‘á»ƒ xem foreign key constraints
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
                _logger.LogError(ex, "Lá»—i khi láº¥y foreign keys");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y foreign keys", error = ex.Message });
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n download backup");
                // }

                // Validate filename Ä‘á»ƒ trÃ¡nh path traversal
                if (string.IsNullOrWhiteSpace(fileName) || 
                    fileName.Contains("..") || 
                    fileName.Contains("\\") || 
                    fileName.Contains("/"))
                {
                    return BadRequest(new { message = "TÃªn file khÃ´ng há»£p lá»‡" });
                }

                // TÃ¬m file trong cÃ¡c thÆ° má»¥c backup cÃ³ thá»ƒ
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "backups", fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "TempBackups", fileName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetailPoint", "Temp", fileName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), fileName)
                };

                // TÃ¬m file tá»« lá»‹ch sá»­ backup
                var backupHistory = await _context.BackupHistories
                    .Where(bh => bh.FileName == fileName && bh.Status == "Success")
                    .OrderByDescending(bh => bh.BackupDate)
                    .FirstOrDefaultAsync();

                string filePath = null;

                // Æ¯u tiÃªn Ä‘Æ°á»ng dáº«n tá»« lá»‹ch sá»­ backup
                if (backupHistory != null && !string.IsNullOrEmpty(backupHistory.FilePath) && 
                    System.IO.File.Exists(backupHistory.FilePath))
                {
                    filePath = backupHistory.FilePath;
                }
                else
                {
                    // TÃ¬m trong cÃ¡c Ä‘Æ°á»ng dáº«n cÃ³ thá»ƒ
                    filePath = possiblePaths.FirstOrDefault(path => System.IO.File.Exists(path));
                }

                if (filePath == null)
                {
                    return NotFound(new { message = $"KhÃ´ng tÃ¬m tháº¥y file backup: {fileName}" });
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
                _logger.LogError(ex, "Lá»—i khi download backup file: {FileName}", fileName);
                return StatusCode(500, new { message = "Lá»—i khi download backup file", error = ex.Message });
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

        // Backup vá»›i tÃ¹y chá»n download trá»±c tiáº¿p
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n sao lÆ°u dá»¯ liá»‡u");
                // }
                
                _logger.LogInformation("Starting backup-and-download operation");
                
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogError("Connection string is null or empty");
                    return BadRequest(new { message = "Connection string khÃ´ng Ä‘Æ°á»£c cáº¥u hÃ¬nh" });
                }
                
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                
                _logger.LogInformation($"Database name: {databaseName}");

                // Táº¡o tÃªn file backup vá»›i timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_backup_{timestamp}.bak";
                
                // Sá»­ dá»¥ng C:\temp - thÆ° má»¥c cÃ³ quyá»n ghi rá»™ng rÃ£i
                string tempBackupPath;
                
                // Táº¡o thÆ° má»¥c C:\temp náº¿u chÆ°a tá»“n táº¡i
                var tempDir = @"C:\temp";
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                tempBackupPath = Path.Combine(tempDir, backupFileName);
                _logger.LogInformation($"Using C:\\temp directory for backup: {tempBackupPath}");

                // CÃ¢u lá»‡nh backup SQL
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
                        command.CommandTimeout = 300; // 5 phÃºt timeout
                        await command.ExecuteNonQueryAsync();
                    }
                }
                _logger.LogInformation("Backup command completed");

                // Kiá»ƒm tra file cÃ³ tá»“n táº¡i khÃ´ng
                if (!System.IO.File.Exists(tempBackupPath))
                {
                    _logger.LogError($"Backup file not found at: {tempBackupPath}");
                    return StatusCode(500, new { message = "File backup khÃ´ng Ä‘Æ°á»£c táº¡o" });
                }

                // Láº¥y thÃ´ng tin file backup
                var fileInfo = new FileInfo(tempBackupPath);
                var fileSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
                _logger.LogInformation($"Backup file size: {fileSizeMB} MB");

                // Skip saving backup history for now to avoid database issues
                _logger.LogInformation("Skipping backup history save for debugging");

                // Äá»c file Ä‘á»ƒ download
                var fileBytes = await System.IO.File.ReadAllBytesAsync(tempBackupPath);
                _logger.LogInformation($"File read successfully, {fileBytes.Length} bytes");
                
                // XÃ³a file táº¡m sau khi Ä‘á»c
                try
                {
                    System.IO.File.Delete(tempBackupPath);
                    _logger.LogInformation("Temp file deleted");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "KhÃ´ng thá»ƒ xÃ³a file táº¡m: {TempPath}", tempBackupPath);
                }

                // Thiáº¿t láº­p Content-Disposition header cho download
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{backupFileName}\"");
                
                return File(fileBytes, "application/octet-stream", backupFileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Lá»—i quyá»n truy cáº­p khi backup vÃ  download database");
                return StatusCode(500, new { 
                    message = "KhÃ´ng cÃ³ quyá»n truy cáº­p thÆ° má»¥c backup", 
                    error = "Server khÃ´ng cÃ³ quyá»n ghi vÃ o thÆ° má»¥c. Vui lÃ²ng liÃªn há»‡ quáº£n trá»‹ viÃªn.",
                    details = ex.Message 
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "KhÃ´ng tÃ¬m tháº¥y thÆ° má»¥c backup");
                return StatusCode(500, new { 
                    message = "ThÆ° má»¥c backup khÃ´ng tá»“n táº¡i", 
                    error = "KhÃ´ng thá»ƒ táº¡o thÆ° má»¥c backup. Vui lÃ²ng liÃªn há»‡ quáº£n trá»‹ viÃªn.",
                    details = ex.Message 
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lá»—i SQL khi backup database");
                return StatusCode(500, new { 
                    message = "Lá»—i database khi táº¡o backup", 
                    error = "Kiá»ƒm tra káº¿t ná»‘i database vÃ  quyá»n backup",
                    details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi backup vÃ  download database");
                return StatusCode(500, new { 
                    message = "Lá»—i khi backup vÃ  download database", 
                    error = ex.Message, 
                    details = ex.ToString() 
                });
            }
        }

        // Debug endpoint Ä‘á»ƒ kiá»ƒm tra request
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

        // Test backup endpoint Ä‘á»ƒ kiá»ƒm tra SQL Server permissions
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
                
                // Táº¡o thÆ° má»¥c náº¿u chÆ°a tá»“n táº¡i
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
                    message = "Test backup thÃ nh cÃ´ng",
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
                    message = "Test backup tháº¥t báº¡i", 
                    error = ex.Message,
                    details = ex.ToString()
                });
            }
        }

        // Debug endpoint Ä‘á»ƒ kiá»ƒm tra SQL Server backup directory
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

        // Backup toÃ n bá»™ database
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n sao lÆ°u dá»¯ liá»‡u");
                // }
                
                _logger.LogInformation($"Backup request received - BackupPath: '{request.BackupPath}'");
                
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                var serverName = sqlConnectionStringBuilder.DataSource;

                // Táº¡o tÃªn file backup vá»›i timestamp
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_backup_{timestamp}.bak";
                
                string backupPath;
                if (!string.IsNullOrWhiteSpace(request.BackupPath))
                {
                    // Sá»­ dá»¥ng Ä‘Æ°á»ng dáº«n do ngÆ°á»i dÃ¹ng chá»‰ Ä‘á»‹nh
                    backupPath = Path.Combine(request.BackupPath, backupFileName);
                }
                else
                {
                    // Sá»­ dá»¥ng C:\temp - thÆ° má»¥c cÃ³ quyá»n ghi rá»™ng rÃ£i
                    var tempDir = @"C:\temp";
                    if (!Directory.Exists(tempDir))
                    {
                        Directory.CreateDirectory(tempDir);
                    }
                    
                    backupPath = Path.Combine(tempDir, backupFileName);
                    _logger.LogInformation($"Using C:\\temp directory for backup: {backupPath}");
                }
                
                _logger.LogInformation($"Final backup path: '{backupPath}");

                // CÃ¢u lá»‡nh backup SQL
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
                        command.CommandTimeout = 300; // 5 phÃºt timeout
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Láº¥y thÃ´ng tin file backup
                var fileInfo = new FileInfo(backupPath);
                var fileSizeMB = Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);

                // LÆ°u lá»‹ch sá»­ backup vÃ o database
                var backupHistory = new BackupHistory
                {
                    BackupDate = DateTime.Now,
                    BackupType = "Manual",
                    FilePath = backupPath,
                    FileName = backupFileName,
                    FileSizeMB = fileSizeMB,
                    Status = "Success",
                    Note = "Backup thá»§ cÃ´ng tá»« Data Management"
                };

                _context.BackupHistories.Add(backupHistory);
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Backup database thÃ nh cÃ´ng", 
                    backupPath = backupPath,
                    fileName = backupFileName,
                    timestamp = timestamp,
                    size = fileSizeMB
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Lá»—i quyá»n truy cáº­p khi backup database");
                return StatusCode(500, new { 
                    message = "KhÃ´ng cÃ³ quyá»n truy cáº­p thÆ° má»¥c backup", 
                    error = "Vui lÃ²ng kiá»ƒm tra quyá»n ghi file hoáº·c chá»n thÆ° má»¥c khÃ¡c",
                    details = ex.Message 
                });
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError(ex, "KhÃ´ng tÃ¬m tháº¥y thÆ° má»¥c backup");
                return StatusCode(500, new { 
                    message = "ThÆ° má»¥c backup khÃ´ng tá»“n táº¡i", 
                    error = "Vui lÃ²ng táº¡o thÆ° má»¥c hoáº·c chá»n Ä‘Æ°á»ng dáº«n khÃ¡c",
                    details = ex.Message 
                });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Lá»—i SQL khi backup database");
                return StatusCode(500, new { 
                    message = "Lá»—i database khi táº¡o backup", 
                    error = "Kiá»ƒm tra káº¿t ná»‘i database vÃ  quyá»n backup",
                    details = ex.Message 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi backup database");
                return StatusCode(500, new { 
                    message = "Lá»—i khi backup database", 
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xem danh sÃ¡ch backup");
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
                _logger.LogError(ex, "Lá»—i khi láº¥y danh sÃ¡ch backup files");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y danh sÃ¡ch backup files", error = ex.Message });
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xem lá»‹ch sá»­ backup");
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
                _logger.LogError(ex, "Lá»—i khi láº¥y lá»‹ch sá»­ backup");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y lá»‹ch sá»­ backup", error = ex.Message });
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n phá»¥c há»“i dá»¯ liá»‡u");
                // }

                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { message = "Vui lÃ²ng chá»n file backup" });
                }

                // Validate file extension
                var allowedExtensions = new[] { ".bak", ".sql" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Chá»‰ cháº¥p nháº­n file .bak hoáº·c .sql" });
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
                    message = "Upload file backup thÃ nh cÃ´ng",
                    filePath = filePath,
                    fileName = fileName,
                    originalName = file.FileName,
                    size = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi upload backup file");
                return StatusCode(500, new { message = "Lá»—i khi upload backup file", error = ex.Message });
            }
        }

        // Restore database tá»« file backup
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n phá»¥c há»“i dá»¯ liá»‡u");
                // }

                if (string.IsNullOrWhiteSpace(request.BackupFilePath))
                {
                    return BadRequest(new { message = "ÄÆ°á»ng dáº«n file backup khÃ´ng Ä‘Æ°á»£c Ä‘á»ƒ trá»‘ng" });
                }

                if (!System.IO.File.Exists(request.BackupFilePath))
                {
                    return BadRequest(new { 
                        message = "File backup khÃ´ng tá»“n táº¡i", 
                        filePath = request.BackupFilePath,
                        suggestion = "Vui lÃ²ng upload file backup trÆ°á»›c khi thá»±c hiá»‡n restore"
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

                // ÄÃ³ng táº¥t cáº£ káº¿t ná»‘i Ä‘áº¿n database báº±ng cÃ¡ch kill sessions
                var killConnectionsQuery = $@"
                    DECLARE @kill varchar(8000) = '';  
                    SELECT @kill = @kill + 'KILL ' + CONVERT(varchar(5), session_id) + ';'  
                    FROM sys.dm_exec_sessions
                    WHERE database_id = DB_ID('{databaseName}') AND session_id <> @@SPID;
                    EXEC(@kill);";

                // CÃ¢u lá»‡nh restore SQL vá»›i REPLACE Ä‘á»ƒ ghi Ä‘Ã¨ database hiá»‡n táº¡i
                var restoreQuery = $@"
                    RESTORE DATABASE [{databaseName}] 
                    FROM DISK = N'{request.BackupFilePath}' 
                    WITH REPLACE, STATS = 10";

                _logger.LogInformation("Starting database restore process for file: {FilePath}", request.BackupFilePath);
                _logger.LogInformation("Restore SQL Query: {Query}", restoreQuery);

                // ÄÃ³ng Entity Framework connection trÆ°á»›c
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
                        
                        // ÄÃ³ng cÃ¡c káº¿t ná»‘i khÃ¡c trÆ°á»›c
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
                            restoreCommand.CommandTimeout = 600; // 10 phÃºt timeout
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

                // LÃ m sáº¡ch Entity Framework context sau khi restore thÃ nh cÃ´ng
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
                    message = "Restore database thÃ nh cÃ´ng",
                    restoredFrom = request.BackupFilePath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi restore database. File: {BackupFilePath}, Error: {ErrorMessage}", request.BackupFilePath, ex.Message);
                
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
                    message = "Lá»—i khi restore database", 
                    error = ex.Message,
                    details = ex.InnerException?.Message,
                    filePath = request.BackupFilePath
                });
            }
        }

        // Simple delete method - xÃ³a má»™t cÃ¡ch an toÃ n
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");
                // }

                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lÃ²ng nháº­p Ä‘Ãºng text xÃ¡c nháº­n: DELETE SALES DATA" });
                }

                var result = new List<object>();

                // XÃ³a tá»«ng báº£ng má»™t cÃ¡ch Ä‘Æ¡n giáº£n
                try
                {
                    // 1. Äáº¿m records trÆ°á»›c khi xÃ³a
                    var orderItemsCount = await _context.OrderItems.CountAsync();
                    var ordersCount = await _context.Orders.CountAsync();
                    var customersCount = await _context.Customers.CountAsync();
                    var inventoryCount = await _context.InventoryTransactions.CountAsync();

                    result.Add(new { action = "count_before", orderItems = orderItemsCount, orders = ordersCount, customers = customersCount, inventory = inventoryCount });

                    // 2. XÃ³a OrderItems
                    if (orderItemsCount > 0)
                    {
                        var orderItems = await _context.OrderItems.ToListAsync();
                        _context.OrderItems.RemoveRange(orderItems);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "OrderItems", count = orderItemsCount });
                    }

                    // 3. XÃ³a Orders
                    if (ordersCount > 0)
                    {
                        var orders = await _context.Orders.ToListAsync();
                        _context.Orders.RemoveRange(orders);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "Orders", count = ordersCount });
                    }

                    // 4. XÃ³a Customers
                    if (customersCount > 0)
                    {
                        var customers = await _context.Customers.ToListAsync();
                        _context.Customers.RemoveRange(customers);
                        await _context.SaveChangesAsync();
                        result.Add(new { action = "deleted", table = "Customers", count = customersCount });
                    }

                    // 5. XÃ³a InventoryTransactions
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

                    // 7. Äáº¿m láº¡i Ä‘á»ƒ kiá»ƒm tra
                    var remainingOrderItems = await _context.OrderItems.CountAsync();
                    var remainingOrders = await _context.Orders.CountAsync();
                    var remainingCustomers = await _context.Customers.CountAsync();
                    var remainingInventory = await _context.InventoryTransactions.CountAsync();

                    result.Add(new { action = "count_after", orderItems = remainingOrderItems, orders = remainingOrders, customers = remainingCustomers, inventory = remainingInventory });

                    var success = remainingOrderItems == 0 && remainingOrders == 0 && remainingCustomers == 0 && remainingInventory == 0;

                    return Ok(new
                    {
                        success = success,
                        message = success ? "XÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng thÃ nh cÃ´ng!" : "CÃ³ má»™t sá»‘ dá»¯ liá»‡u chÆ°a Ä‘Æ°á»£c xÃ³a hoÃ n toÃ n",
                        details = result,
                        timestamp = DateTime.Now
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng Ä‘Æ¡n giáº£n");
                    return StatusCode(500, new { message = "Lá»—i khi xÃ³a dá»¯ liá»‡u", error = ex.Message, details = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i tá»•ng quÃ¡t khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");
                return StatusCode(500, new { message = "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng", error = ex.Message });
            }
        }

        // Ultra safe delete method - xÃ³a tá»«ng record má»™t
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");
                // }

                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lÃ²ng nháº­p Ä‘Ãºng text xÃ¡c nháº­n: DELETE SALES DATA" });
                }

                var result = new List<object>();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // XÃ³a theo thá»© tá»± an toÃ n báº±ng raw SQL vá»›i transaction
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // 1. XÃ³a OrderItems (child table)
                            var orderItemsCmd = new SqlCommand("SELECT COUNT(*) FROM OrderItems", connection, transaction);
                            var orderItemsCount = (int)(await orderItemsCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (orderItemsCount > 0)
                            {
                                var deleteOrderItemsCmd = new SqlCommand("DELETE FROM OrderItems", connection, transaction);
                                var deletedOrderItems = await deleteOrderItemsCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "OrderItems", deletedCount = deletedOrderItems });
                            }

                            // 2. XÃ³a Orders (parent table)
                            var ordersCmd = new SqlCommand("SELECT COUNT(*) FROM Orders", connection, transaction);
                            var ordersCount = (int)(await ordersCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (ordersCount > 0)
                            {
                                var deleteOrdersCmd = new SqlCommand("DELETE FROM Orders", connection, transaction);
                                var deletedOrders = await deleteOrdersCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Orders", deletedCount = deletedOrders });
                            }

                            // 3. XÃ³a Customers
                            var customersCmd = new SqlCommand("SELECT COUNT(*) FROM Customers", connection, transaction);
                            var customersCount = (int)(await customersCmd.ExecuteScalarAsync() ?? 0);
                            
                            if (customersCount > 0)
                            {
                                var deleteCustomersCmd = new SqlCommand("DELETE FROM Customers", connection, transaction);
                                var deletedCustomers = await deleteCustomersCmd.ExecuteNonQueryAsync();
                                result.Add(new { table = "Customers", deletedCount = deletedCustomers });
                            }

                            // 4. XÃ³a InventoryTransactions
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
                                message = "ÄÃ£ xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng thÃ nh cÃ´ng (ultra safe method)", 
                                details = result,
                                timestamp = DateTime.Now 
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            _logger.LogError(ex, "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng (ultra safe)");
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng (ultra safe)");
                return StatusCode(500, new { message = "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng", error = ex.Message });
            }
        }

        // XÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng (giá»¯ láº¡i sáº£n pháº©m, nhÃ³m hÃ ng)
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");
                // }
                if (confirmation.ConfirmationText != "DELETE SALES DATA")
                {
                    return BadRequest(new { message = "Vui lÃ²ng nháº­p Ä‘Ãºng text xÃ¡c nháº­n: DELETE SALES DATA" });
                }

                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _logger.LogInformation("Báº¯t Ä‘áº§u xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng...");

                        // Danh sÃ¡ch báº£ng cáº§n xÃ³a theo thá»© tá»± (tá»« child Ä‘áº¿n parent)
                        var tablesToDelete = new[]
                        {
                            // Dá»¯ liá»‡u chi tiáº¿t Ä‘Æ¡n hÃ ng
                            "OrderItems",
                            
                            // Dá»¯ liá»‡u Ä‘Æ¡n hÃ ng
                            "Orders",
                            
                            // Dá»¯ liá»‡u khÃ¡ch hÃ ng
                            "Customers",
                            
                            // Dá»¯ liá»‡u kho vÃ  giao dá»‹ch
                            "InventoryMovements",
                            "InventoryTransactions",
                            
                            // Dá»¯ liá»‡u thanh toÃ¡n
                            "PaymentTransactions",
                            "PaymentStats",
                            
                            // Dá»¯ liá»‡u bÃ¡o cÃ¡o
                            "SalesReports",
                            "DailySalesReports",
                            "MonthlySalesReports",
                            "ProductSalesReports",
                            
                            // HÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­
                            "EInvoiceItems",
                            "EInvoices",
                            
                            // ThÃ´ng bÃ¡o liÃªn quan Ä‘áº¿n bÃ¡n hÃ ng
                            "Notifications",
                            
                            // Log activities liÃªn quan
                            "ActivityLogs",
                            "AuditLogs"
                        };

                        var deletedTables = new List<string>();
                        var skippedTables = new List<string>();

                        // Táº¯t foreign key constraints táº¡m thá»i
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT ALL'");
                            _logger.LogInformation("ÄÃ£ táº¯t foreign key constraints");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Lá»—i khi táº¯t constraints: {ex.Message}");
                        }

                        // XÃ³a tá»«ng báº£ng (khÃ´ng cáº§n kiá»ƒm tra tá»“n táº¡i, sáº½ catch exception náº¿u khÃ´ng cÃ³)
                        foreach (var table in tablesToDelete)
                        {
                            try
                            {
                                // Table name is from predefined safe array, not user input
                                #pragma warning disable EF1002
                                var result = await _context.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}] WHERE 1=1");
                                #pragma warning restore EF1002
                                deletedTables.Add($"{table}");
                                _logger.LogInformation($"ÄÃ£ xÃ³a {table}");
                            }
                            catch (Exception ex)
                            {
                                skippedTables.Add($"{table} (lá»—i: {ex.Message})");
                                _logger.LogWarning($"Lá»—i khi xÃ³a {table}: {ex.Message}");
                            }
                        }
                        
                        // Reset inventory quantities vá» 0
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync("UPDATE Products SET StockQuantity = 0 WHERE StockQuantity IS NOT NULL");
                            _logger.LogInformation("ÄÃ£ reset StockQuantity");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Lá»—i khi reset StockQuantity: {ex.Message}");
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
                                _logger.LogInformation($"ÄÃ£ reset identity cho {table}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Lá»—i khi reset identity cho {table}: {ex.Message}");
                            }
                        }

                        // Báº­t láº¡i foreign key constraints
                        try
                        {
                            await _context.Database.ExecuteSqlRawAsync("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL'");
                            _logger.LogInformation("ÄÃ£ báº­t láº¡i foreign key constraints");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Lá»—i khi báº­t láº¡i constraints: {ex.Message}");
                        }

                        await transaction.CommitAsync();
                        _logger.LogInformation("HoÃ n thÃ nh xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");

                        return Ok(new { 
                            message = "ÄÃ£ xÃ³a toÃ n bá»™ dá»¯ liá»‡u bÃ¡n hÃ ng thÃ nh cÃ´ng. Sáº£n pháº©m vÃ  cáº¥u hÃ¬nh há»‡ thá»‘ng Ä‘Æ°á»£c giá»¯ láº¡i.",
                            timestamp = DateTime.Now,
                            deletedTables = deletedTables,
                            skippedTables = skippedTables,
                            note = "ÄÃ£ xÃ³a: Ä‘Æ¡n hÃ ng, khÃ¡ch hÃ ng, giao dá»‹ch kho, thanh toÃ¡n, bÃ¡o cÃ¡o, hÃ³a Ä‘Æ¡n Ä‘iá»‡n tá»­, thÃ´ng bÃ¡o"
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
                _logger.LogError(ex, "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng");
                return StatusCode(500, new { message = "Lá»—i khi xÃ³a dá»¯ liá»‡u bÃ¡n hÃ ng", error = ex.Message });
            }
        }

        // Láº¥y thÃ´ng tin database
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
                //     return Forbid("Báº¡n khÃ´ng cÃ³ quyá»n xem thÃ´ng tin dá»¯ liá»‡u");
                // }

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new { message = "Connection string khÃ´ng Ä‘Æ°á»£c cáº¥u hÃ¬nh" });
                }

                try
                {
                    var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                    var databaseName = sqlConnectionStringBuilder.InitialCatalog;
                    var serverName = sqlConnectionStringBuilder.DataSource;
                    
                    // Láº¥y thÃ´ng tin backup cuá»‘i cÃ¹ng tá»« BackupHistory
                    var lastBackup = await _context.BackupHistories
                        .Where(bh => bh.Status == "Success")
                        .OrderByDescending(bh => bh.BackupDate)
                        .FirstOrDefaultAsync();

                    string lastBackupInfo = "ChÆ°a cÃ³ thÃ´ng tin";
                    if (lastBackup != null)
                    {
                        var timeAgo = DateTime.Now - lastBackup.BackupDate;
                        if (timeAgo.TotalMinutes < 60)
                        {
                            lastBackupInfo = $"{(int)timeAgo.TotalMinutes} phÃºt trÆ°á»›c ({lastBackup.BackupType})";
                        }
                        else if (timeAgo.TotalHours < 24)
                        {
                            lastBackupInfo = $"{(int)timeAgo.TotalHours} giá» trÆ°á»›c ({lastBackup.BackupType})";
                        }
                        else
                        {
                            lastBackupInfo = $"{(int)timeAgo.TotalDays} ngÃ y trÆ°á»›c ({lastBackup.BackupType})";
                        }
                    }

                    // Láº¥y kÃ­ch thÆ°á»›c database
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
                        _logger.LogWarning(sizeEx, "KhÃ´ng thá»ƒ láº¥y kÃ­ch thÆ°á»›c database");
                    }
                    
                    // Sau khi restore, Entity Framework context cÃ³ thá»ƒ bá»‹ lá»—i
                    // Thá»­ refresh context báº±ng cÃ¡ch táº¡o connection má»›i
                    try
                    {
                        // Sá»­ dá»¥ng Entity Framework Ä‘á»ƒ láº¥y thÃ´ng tin Ä‘Æ¡n giáº£n
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
                        
                        // Fallback: Sá»­ dá»¥ng direct SQL connection
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
                    _logger.LogError(sqlEx, "Lá»—i káº¿t ná»‘i SQL Server");
                    
                    // Fallback: tráº£ vá» thÃ´ng tin cÆ¡ báº£n tá»« connection string
                    try
                    {
                        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
                        
                        // Váº«n cá»‘ gáº¯ng láº¥y thÃ´ng tin backup cuá»‘i tá»« BackupHistory
                        var lastBackup = await _context.BackupHistories
                            .Where(bh => bh.Status == "Success")
                            .OrderByDescending(bh => bh.BackupDate)
                            .FirstOrDefaultAsync();

                        string lastBackupInfo = "ChÆ°a cÃ³ thÃ´ng tin";
                        if (lastBackup != null)
                        {
                            var timeAgo = DateTime.Now - lastBackup.BackupDate;
                            if (timeAgo.TotalMinutes < 60)
                            {
                                lastBackupInfo = $"{(int)timeAgo.TotalMinutes} phÃºt trÆ°á»›c ({lastBackup.BackupType})";
                            }
                            else if (timeAgo.TotalHours < 24)
                            {
                                lastBackupInfo = $"{(int)timeAgo.TotalHours} giá» trÆ°á»›c ({lastBackup.BackupType})";
                            }
                            else
                            {
                                lastBackupInfo = $"{(int)timeAgo.TotalDays} ngÃ y trÆ°á»›c ({lastBackup.BackupType})";
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
                            lastBackup = "KhÃ´ng thá»ƒ láº¥y thÃ´ng tin",
                            lastBackupDate = (DateTime?)null,
                            lastBackupType = (string?)null,
                            lastBackupSize = (double?)null
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lá»—i khi láº¥y thÃ´ng tin database");
                return StatusCode(500, new { message = "Lá»—i khi láº¥y thÃ´ng tin database", error = ex.Message });
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
