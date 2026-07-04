using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    public class DriverPaymentsController : Controller
    {
        private readonly IDriverPaymentService _driverPaymentService;

        public DriverPaymentsController(IDriverPaymentService driverPaymentService)
        {
            _driverPaymentService = driverPaymentService;
        }

        // GET: DriverPayments
        public async Task<IActionResult> Index()
        {
            var payments = await _driverPaymentService.GetDriverPaymentsAsync();
            return View(payments);
        }

        // GET: DriverPayments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driverPayment = await _driverPaymentService.GetDriverPaymentByIdAsync(id.Value);
            if (driverPayment == null)
            {
                return NotFound();
            }

            return View(driverPayment);
        }

        // GET: DriverPayments/Create
        public async Task<IActionResult> Create()
        {
            ViewData["DriverId"] = new SelectList(await _driverPaymentService.GetDriversAsync(), "DriverId", "DriverId");
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
                await _driverPaymentService.CreateDriverPaymentAsync(driverPayment);
                return RedirectToAction(nameof(Index));
            }
            ViewData["DriverId"] = new SelectList(await _driverPaymentService.GetDriversAsync(), "DriverId", "DriverId", driverPayment.DriverId);
            return View(driverPayment);
        }

        // GET: DriverPayments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driverPayment = await _driverPaymentService.GetDriverPaymentByIdAsync(id.Value);
            if (driverPayment == null)
            {
                return NotFound();
            }
            ViewData["DriverId"] = new SelectList(await _driverPaymentService.GetDriversAsync(), "DriverId", "DriverId", driverPayment.DriverId);
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
                    await _driverPaymentService.UpdateDriverPaymentAsync(driverPayment);
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
            ViewData["DriverId"] = new SelectList(await _driverPaymentService.GetDriversAsync(), "DriverId", "DriverId", driverPayment.DriverId);
            return View(driverPayment);
        }

        // GET: DriverPayments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driverPayment = await _driverPaymentService.GetDriverPaymentForDeleteAsync(id.Value);
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
            await _driverPaymentService.DeleteDriverPaymentAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool DriverPaymentExists(int id)
        {
            return _driverPaymentService.DriverPaymentExists(id);
        }
    }
}
