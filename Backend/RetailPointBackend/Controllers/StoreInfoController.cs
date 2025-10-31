using Microsoft.AspNetCore.Mvc;
using RetailPointBackend.Models;
using System.Linq;

namespace RetailPointBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StoreInfoController : ControllerBase
    {
        private readonly AppDbContext _context;
        public StoreInfoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/StoreInfo
        [HttpGet]
        public IActionResult GetStoreInfo()
        {
            var info = _context.StoreInfos.FirstOrDefault();
            if (info == null) return NotFound();
            return Ok(info);
        }

        // POST: api/StoreInfo
        [HttpPost]
        public IActionResult UpsertStoreInfo([FromBody] StoreInfo model)
        {
            var existing = _context.StoreInfos.FirstOrDefault();
            if (existing != null)
            {
                existing.Name = model.Name;
                existing.Address = model.Address;
                existing.TaxCode = model.TaxCode;
                existing.Phone = model.Phone;
                existing.Email = model.Email;
                existing.BankAccountName = model.BankAccountName;
                existing.BankAccountNumber = model.BankAccountNumber;
                existing.BankName = model.BankName;
                existing.BankBranch = model.BankBranch;
                _context.StoreInfos.Update(existing);
            }
            else
            {
                _context.StoreInfos.Add(model);
            }
            _context.SaveChanges();
            return Ok(model);
        }
    }
}
