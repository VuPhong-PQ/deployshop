using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Models;
using System.Linq;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaxConfigController : ControllerBase
    {
        private readonly AppDbContext _context;
        public TaxConfigController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/TaxConfig
        [HttpGet]
        public IActionResult GetTaxConfig()
        {
            var config = _context.TaxConfigs.FirstOrDefault();
            if (config == null) return NotFound();
            return Ok(config);
        }

        // POST: api/TaxConfig
        [HttpPost]
        public IActionResult UpsertTaxConfig([FromBody] TaxConfig model)
        {
            var existing = _context.TaxConfigs.FirstOrDefault();
            if (existing != null)
            {
                existing.EnableVAT = model.EnableVAT;
                existing.VATIncludedInPrice = model.VATIncludedInPrice;
                existing.VATRate = model.VATRate;
                existing.VATLabel = model.VATLabel;
                existing.EnableEnvTax = model.EnableEnvTax;
                existing.EnvTaxRate = model.EnvTaxRate;
                _context.TaxConfigs.Update(existing);
            }
            else
            {
                _context.TaxConfigs.Add(model);
            }
            _context.SaveChanges();
            return Ok(model);
        }
    }
}
