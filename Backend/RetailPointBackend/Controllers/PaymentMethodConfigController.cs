using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RetailPointBackend.Models;
using System.Linq;

namespace RetailPointBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentMethodConfigController : ControllerBase
    {
        private readonly AppDbContext _context;
        public PaymentMethodConfigController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetConfig()
        {
            var config = _context.PaymentMethodConfigs.FirstOrDefault();
            if (config == null) return NotFound();
            return Ok(config);
        }

        [HttpGet("enabled")]
        public IActionResult GetEnabledPaymentMethods()
        {
            var config = _context.PaymentMethodConfigs.FirstOrDefault();
            if (config == null)
            {
                return Ok(new
                {
                    paymentMethods = new[]
                    {
                        new { id = "cash", name = "Tiền mặt", enabled = true }
                    },
                    defaultMethod = "cash"
                });
            }

            var enabledMethods = new List<object>();

            if (config.EnableCash)
                enabledMethods.Add(new { id = "cash", name = "Tiền mặt", enabled = true });
            if (config.EnableBankCard)
                enabledMethods.Add(new { id = "card", name = "Thẻ ngân hàng", enabled = true });
            if (config.EnableQRCode)
                enabledMethods.Add(new { id = "qr", name = "QR Code", enabled = true });
            if (config.EnableEWallet)
                enabledMethods.Add(new { id = "ewallet", name = "USD Ngoại Tệ", enabled = true });
            if (config.EnableBankTransfer)
                enabledMethods.Add(new { id = "banktransfer", name = "Chuyển khoản", enabled = true });
            if (config.EnableForeignUSD)
                enabledMethods.Add(new { id = "foreignusd", name = "Ngoại tệ USD", enabled = true });
            if (config.EnableForeignEUR)
                enabledMethods.Add(new { id = "foreigneur", name = "Ngoại tệ EUR", enabled = true });

            return Ok(new
            {
                paymentMethods = enabledMethods,
                defaultMethod = config.DefaultMethod,
                enablePartialPayment = config.EnablePartialPayment,
                enableDrawer = config.EnableDrawer
            });
        }

        [HttpPost]
        public IActionResult UpsertConfig([FromBody] PaymentMethodConfig model)
        {
            var existing = _context.PaymentMethodConfigs.FirstOrDefault();
            if (existing != null)
            {
                existing.EnableCash = model.EnableCash;
                existing.EnableBankCard = model.EnableBankCard;
                existing.EnableQRCode = model.EnableQRCode;
                existing.EnableEWallet = model.EnableEWallet;
                existing.EnableBankTransfer = model.EnableBankTransfer;
                existing.EnableForeignUSD = model.EnableForeignUSD;
                existing.EnableForeignEUR = model.EnableForeignEUR;
                existing.EnablePartialPayment = model.EnablePartialPayment;
                existing.EnableDrawer = model.EnableDrawer;
                existing.DefaultMethod = model.DefaultMethod;
                _context.PaymentMethodConfigs.Update(existing);
            }
            else
            {
                _context.PaymentMethodConfigs.Add(model);
            }
            _context.SaveChanges();
            return Ok(model);
        }
    }
}