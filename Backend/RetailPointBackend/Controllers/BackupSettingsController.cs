using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using RetailPointBackend.BackgroundServices;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackupSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly BackupScheduleBackgroundService _backupService;

        public BackupSettingsController(AppDbContext context, BackupScheduleBackgroundService backupService)
        {
            _context = context;
            _backupService = backupService;
        }

        // GET: api/BackupSettings
        [HttpGet]
        public async Task<ActionResult<BackupSettings>> GetBackupSettings()
        {
            try
            {
                var settings = await _context.BackupSettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Create default settings if none exist
                    settings = new BackupSettings
                    {
                        BackupTime = new TimeSpan(13, 0, 0), // 1:00 PM
                        IsEnabled = true,
                        Notes = "Default backup settings"
                    };
                    
                    _context.BackupSettings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error getting backup settings: {ex.Message}");
            }
        }

        // PUT: api/BackupSettings
        [HttpPut]
        public async Task<IActionResult> UpdateBackupSettings([FromBody] BackupSettingsRequest request)
        {
            try
            {
                var settings = await _context.BackupSettings.FirstOrDefaultAsync();
                
                if (settings == null)
                {
                    // Create new settings if none exist
                    settings = new BackupSettings();
                    _context.BackupSettings.Add(settings);
                }

                // Parse time from request
                if (!TimeSpan.TryParse(request.BackupTime, out TimeSpan backupTime))
                {
                    return BadRequest("Invalid time format. Use HH:mm format.");
                }

                settings.BackupTime = backupTime;
                settings.IsEnabled = request.IsEnabled;
                settings.Notes = request.Notes;
                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Update the background service with new settings
                _backupService.UpdateSchedule(backupTime, request.IsEnabled);

                return Ok(new { message = "Backup settings updated successfully", settings });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating backup settings: {ex.Message}");
            }
        }

        // POST: api/BackupSettings/test
        [HttpPost("test")]
        public async Task<IActionResult> TestBackup()
        {
            try
            {
                await _backupService.ExecuteBackupAsync();
                return Ok(new { message = "Test backup completed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error running test backup: {ex.Message}");
            }
        }
    }

    public class BackupSettingsRequest
    {
        public string BackupTime { get; set; } = "13:00";
        public bool IsEnabled { get; set; } = true;
        public string? Notes { get; set; }
    }
}