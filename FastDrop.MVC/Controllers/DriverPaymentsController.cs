using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;

namespace FastDrop.MVC
{
    public class DriverPaymentsController : Controller
    {
        private readonly DmsDbContext _context;

        public DriverPaymentsController(DmsDbContext context)
        {
            _context = context;
        }

        // GET: DriverPayments
        public async Task<IActionResult> Index()
        {
            var dmsDbContext = _context.DriverPayments.Include(d => d.Driver);
            return View(await dmsDbContext.ToListAsync());
        }

        // GET: DriverPayments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driverPayment = await _context.DriverPayments
                .Include(d => d.Driver)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (driverPayment == null)
            {
                return NotFound();
            }

            return View(driverPayment);
        }

        // GET: DriverPayments/Create
        public IActionResult Create()
        {
            ViewData["DriverId"] = new SelectList(_context.Drivers, "DriverId", "DriverId");
            return View();
        }

        // POST: DriverPayments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentId,DriverId,TotalPayout,DistanceKm,CreatedAt")] DriverPayment driverPayment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(driverPayment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DriverId"] = new SelectList(_context.Drivers, "DriverId", "DriverId", driverPayment.DriverId);
            return View(driverPayment);
        }

        // GET: DriverPayments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driverPayment = await _context.DriverPayments.FindAsync(id);
            if (driverPayment == null)
            {
                return NotFound();
            }
            ViewData["DriverId"] = new SelectList(_context.Drivers, "DriverId", "DriverId", driverPayment.DriverId);
            return View(driverPayment);
        }

        // POST: DriverPayments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PaymentId,DriverId,TotalPayout,DistanceKm,CreatedAt")] DriverPayment driverPayment)
        {
            if (id != driverPayment.PaymentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(driverPayment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverPaymentExists(driverPayment.PaymentId))
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
            ViewData["DriverId"] = new SelectList(_context.Drivers, "DriverId", "DriverId", driverPayment.DriverId);
            return View(driverPayment);
        }

        // GET: DriverPayments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driverPayment = await _context.DriverPayments
                .Include(d => d.Driver)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (driverPayment == null)
            {
                return NotFound();
            }

            return View(driverPayment);
        }

        // POST: DriverPayments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var driverPayment = await _context.DriverPayments.FindAsync(id);
            if (driverPayment != null)
            {
                _context.DriverPayments.Remove(driverPayment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DriverPaymentExists(int id)
        {
            return _context.DriverPayments.Any(e => e.PaymentId == id);
        }
    }
}
