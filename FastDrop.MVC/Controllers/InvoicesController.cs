using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Employee")]
    public class InvoicesController : Controller
    {
        private readonly DmsDbContext _context;

        public InvoicesController(DmsDbContext context)
        {
            _context = context;
        }

        // GET: Invoices
        public async Task<IActionResult> Index()
        {
            var dmsDbContext = _context.Invoices.Include(i => i.Merchant).Include(i => i.Shipment);
            return View(await dmsDbContext.ToListAsync());
        }

        // GET: Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Merchant)
                .Include(i => i.Shipment)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);
            if (invoice == null)
            {
                return NotFound();
            }

            return View(invoice);
        }

        // GET: Invoices/Create
        public IActionResult Create()
        {
            ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "ShopName");
            return View();
        }

        // POST: Invoices/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(int merchantId, int shipmentCount)
        {
            var shipments = await _context.Shipments
                .Where(s => s.MerchantId == merchantId)
                .OrderByDescending(s => s.CreationDate)
                .Take(shipmentCount)
                .ToListAsync();

            if (!shipments.Any())
            {
                ModelState.AddModelError("", "No shipments found for this merchant.");
                return RedirectToAction(nameof(Create));
            }

            decimal totalAmount = shipments.Sum(s => s.CodAmount);

            var invoice = new Invoice
            {
                MerchantId = merchantId,
                AmountDue = totalAmount,
                PaymentStatus = "Unpaid",
                PeriodStart = shipments.Min(s => s.CreationDate),
                PeriodEnd = shipments.Max(s => s.CreationDate)
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

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
                _context.Add(invoice);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "MerchantId", invoice.MerchantId);
            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId", invoice.ShipmentId);
            return View(invoice);
        }

        // GET: Invoices/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "MerchantId", invoice.MerchantId);
            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId", invoice.ShipmentId);
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
                    _context.Update(invoice);
                    await _context.SaveChangesAsync();
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
            ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "MerchantId", invoice.MerchantId);
            ViewData["ShipmentId"] = new SelectList(_context.Shipments, "ShipmentId", "ShipmentId", invoice.ShipmentId);
            return View(invoice);
        }

        // GET: Invoices/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var invoice = await _context.Invoices
                .Include(i => i.Merchant)
                .Include(i => i.Shipment)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);
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
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceId == id);
        }
    }
}
