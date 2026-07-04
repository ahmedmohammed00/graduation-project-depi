using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    public class PerformanceController : Controller
    {
        private readonly DmsDbContext _context;
        private readonly IPerformanceCalculationService _performanceService;

        public PerformanceController(DmsDbContext context, IPerformanceCalculationService performanceService)
        {
            _context = context;
            _performanceService = performanceService;
        }

        // GET: Performance
        public async Task<IActionResult> Index()
        {
            var dmsDbContext = _context.PerformanceScores.Include(p => p.Driver);
            return View(await dmsDbContext.ToListAsync());
        }

        // GET: Performance/Driver/5
        public async Task<IActionResult> Driver(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var scores = await _context.PerformanceScores
                .Include(p => p.Driver)
                .Where(m => m.DriverId == id)
                .OrderByDescending(m => m.RecordedAt)
                .ToListAsync();

            ViewBag.DriverId = id;
            return View(scores);
        }

        // POST: Performance/Calculate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Calculate(int driverId)
        {
            try
            {
                await _performanceService.CalculateDriverPerformanceAsync(driverId);
                return RedirectToAction(nameof(Driver), new { id = driverId });
            }
            catch (Exception ex)
            {
                // Simple error handling for demonstration
                return Content("Error calculating performance: " + ex.Message);
            }
        }
    }
}
