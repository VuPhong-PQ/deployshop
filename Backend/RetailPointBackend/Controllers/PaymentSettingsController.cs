using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RetailPointBackend.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentSettingsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PaymentSettingsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/PaymentSettings/{storeId}
        [HttpGet("{storeId}")]
        public async Task<IActionResult> Get(string storeId)
        {
            var settings = await _context.PaymentSettings.FirstOrDefaultAsync(x => x.StoreId == storeId);
            if (settings == null) return NotFound();
            return Ok(settings);
        }

        // POST: api/PaymentSettings
        [HttpPost]
        public async Task<IActionResult> Upsert([FromBody] PaymentSettings model)
        {
            if (string.IsNullOrEmpty(model.StoreId)) return BadRequest("StoreId is required");
            var existing = await _context.PaymentSettings.FirstOrDefaultAsync(x => x.StoreId == model.StoreId);
            if (existing != null)
            {
                existing.PaymentMethod = model.PaymentMethod;
                existing.BankAccount = model.BankAccount;
                existing.BankName = model.BankName;
                existing.QrApi = model.QrApi;
                existing.UpdatedAt = DateTime.Now;
                _context.PaymentSettings.Update(existing);
            }
            else
            {
                model.CreatedAt = DateTime.Now;
                model.UpdatedAt = DateTime.Now;
                await _context.PaymentSettings.AddAsync(model);
            }
            await _context.SaveChangesAsync();
            return Ok(model);
        }
    }
}
