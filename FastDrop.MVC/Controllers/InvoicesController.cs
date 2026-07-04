using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Employee")]
    public class InvoicesController : Controller
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var invoices = await _invoiceService.GetInvoicesAsync();
            return View(invoices);
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id.Value);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoices/Create
        public async Task<IActionResult> Create()
        {
            ViewData["MerchantId"] = new SelectList(await _invoiceService.GetMerchantsAsync(), "MerchantId", "ShopName");
            return View();
        }

        // POST: Invoices/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int merchantId, int shipmentCount)
        {
            var invoice = await _invoiceService.GenerateInvoiceAsync(merchantId, shipmentCount);
            
            if (invoice == null)
            {
                ModelState.AddModelError("", "No shipments found for this merchant.");
                return RedirectToAction(nameof(Create));
            }

            return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
        }

        // POST: Invoices/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InvoiceId,ShipmentId,MerchantId,AmountDue,PaymentStatus,PeriodStart,PeriodEnd")] Invoice invoice)
        {
            if (ModelState.IsValid)
            {
                await _invoiceService.CreateInvoiceAsync(invoice);
                return RedirectToAction(nameof(Index));
            }
            ViewData["MerchantId"] = new SelectList(await _invoiceService.GetMerchantsAsync(), "MerchantId", "MerchantId", invoice.MerchantId);
            ViewData["ShipmentId"] = new SelectList(await _invoiceService.GetShipmentsAsync(), "ShipmentId", "ShipmentId", invoice.ShipmentId);
            return View(invoice);
        }

        // GET: Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _invoiceService.GetInvoiceByIdAsync(id.Value);
            if (invoice == null)
            {
                return NotFound();
            }
            ViewData["MerchantId"] = new SelectList(await _invoiceService.GetMerchantsAsync(), "MerchantId", "MerchantId", invoice.MerchantId);
            ViewData["ShipmentId"] = new SelectList(await _invoiceService.GetShipmentsAsync(), "ShipmentId", "ShipmentId", invoice.ShipmentId);
            return View(invoice);
        }

        // POST: Invoices/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("InvoiceId,ShipmentId,MerchantId,AmountDue,PaymentStatus,PeriodStart,PeriodEnd")] Invoice invoice)
        {
            if (id != invoice.InvoiceId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _invoiceService.UpdateInvoiceAsync(invoice);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!InvoiceExists(invoice.InvoiceId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MerchantId"] = new SelectList(await _invoiceService.GetMerchantsAsync(), "MerchantId", "MerchantId", invoice.MerchantId);
            ViewData["ShipmentId"] = new SelectList(await _invoiceService.GetShipmentsAsync(), "ShipmentId", "ShipmentId", invoice.ShipmentId);
            return View(invoice);
        }

        // GET: Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _invoiceService.GetInvoiceForDeleteAsync(id.Value);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // POST: Invoices/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _invoiceService.DeleteInvoiceAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool InvoiceExists(int id)
        {
            return _invoiceService.InvoiceExists(id);
        }
    }
}
