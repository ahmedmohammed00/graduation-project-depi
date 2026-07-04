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
    [Authorize]
    public class ShipmentsController : Controller
    {
        private readonly DmsDbContext _context;

        public ShipmentsController(DmsDbContext context)
        {
            _context = context;
        }

        // GET: Shipments
        public async Task<IActionResult> Index()
        {
            var query = _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .AsQueryable();

            if (User.IsInRole("Merchant"))
            {
                var merchantIdStr = User.Claims.FirstOrDefault(c => c.Type == "MerchantId")?.Value;
                if (int.TryParse(merchantIdStr, out int merchantId))
                {
                    query = query.Where(s => s.MerchantId == merchantId);
                }
            }
            else if (User.IsInRole("Driver"))
            {
                var driverIdStr = User.Claims.FirstOrDefault(c => c.Type == "DriverId")?.Value;
                if (int.TryParse(driverIdStr, out int driverId))
                {
                    // For driver, we show shipments on their assigned routes
                    var assignedRoutes = _context.DriverRoutes.Where(dr => dr.DriverId == driverId).Select(dr => dr.RouteId).ToList();
                    query = query.Where(s => s.RouteId.HasValue && assignedRoutes.Contains(s.RouteId.Value));
                }
            }

            return View(await query.ToListAsync());
        }

        // GET: Shipments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shipment = await _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .Include(s => s.ShipmentStatuses)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);
            if (shipment == null)
            {
                return NotFound();
            }

            return View(shipment);
        }

        [Authorize(Roles = "Employee,Merchant")]
        public IActionResult Create()
        {
            ViewData["AddressId"] = new SelectList(_context.Addresses, "AddressId", "AddressId");
            if (!User.IsInRole("Merchant"))
            {
                ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "ShopName");
            }
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName");
            ViewData["WarehouseId"] = new SelectList(_context.Warehouses, "WarehouseId", "WarehouseName");
            return View();
        }

        // POST: Shipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee,Merchant")]
        public async Task<IActionResult> Create([Bind("ShipmentId,CreationDate,CodAmount,WeightKg,WarehouseId,MerchantId,RouteId,AddressId")] Shipment shipment)
        {
            if (User.IsInRole("Merchant"))
            {
                var merchantIdStr = User.Claims.FirstOrDefault(c => c.Type == "MerchantId")?.Value;
                if (int.TryParse(merchantIdStr, out int merchantId))
                {
                    shipment.MerchantId = merchantId;
                }
                // Clear validation state for MerchantId since we manually assign it
                ModelState.Remove("MerchantId");
            }

            if (ModelState.IsValid)
            {
                _context.Add(shipment);
                
                // Add initial pending status
                _context.ShipmentStatuses.Add(new ShipmentStatus
                {
                    Shipment = shipment,
                    Status = "Pending",
                    UpdatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "AddressId", "AddressId", shipment.AddressId);
            if (!User.IsInRole("Merchant"))
            {
                ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "ShopName", shipment.MerchantId);
            }
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", shipment.RouteId);
            ViewData["WarehouseId"] = new SelectList(_context.Warehouses, "WarehouseId", "WarehouseName", shipment.WarehouseId);
            return View(shipment);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(_context.Addresses, "AddressId", "AddressId", shipment.AddressId);
            ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "ShopName", shipment.MerchantId);
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", shipment.RouteId);
            ViewData["WarehouseId"] = new SelectList(_context.Warehouses, "WarehouseId", "WarehouseName", shipment.WarehouseId);
            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Edit(int id, [Bind("ShipmentId,CreationDate,CodAmount,WeightKg,WarehouseId,MerchantId,RouteId,AddressId")] Shipment shipment)
        {
            if (id != shipment.ShipmentId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(shipment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ShipmentExists(shipment.ShipmentId))
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
            ViewData["AddressId"] = new SelectList(_context.Addresses, "AddressId", "AddressId", shipment.AddressId);
            ViewData["MerchantId"] = new SelectList(_context.Merchants, "MerchantId", "ShopName", shipment.MerchantId);
            ViewData["RouteId"] = new SelectList(_context.Routes, "RouteId", "RouteName", shipment.RouteId);
            ViewData["WarehouseId"] = new SelectList(_context.Warehouses, "WarehouseId", "WarehouseName", shipment.WarehouseId);
            return View(shipment);
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shipment = await _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);
            if (shipment == null)
            {
                return NotFound();
            }

            return View(shipment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment != null)
            {
                _context.Shipments.Remove(shipment);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ShipmentExists(int id)
        {
            return _context.Shipments.Any(e => e.ShipmentId == id);
        }

        // POST: Shipments/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Driver,Employee")]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? comments = null)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return NotFound();
            }

            // If user is a driver, verify it is assigned to their route
            if (User.IsInRole("Driver"))
            {
                var driverIdStr = User.Claims.FirstOrDefault(c => c.Type == "DriverId")?.Value;
                if (int.TryParse(driverIdStr, out int driverId))
                {
                    var isAssigned = await _context.DriverRoutes.AnyAsync(dr => dr.DriverId == driverId && dr.RouteId == shipment.RouteId);
                    if (!isAssigned)
                    {
                        return Forbid();
                    }
                }
            }

            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                Shipment = shipment,
                Status = status,
                UpdatedAt = DateTime.UtcNow
            });

            if (status == "Delivered")
            {
                var attempt = new ShipmentAttempt
                {
                    Shipment = shipment,
                    AttemptNumber = 1,
                    Status = "Success",
                    AttemptTime = DateTime.UtcNow
                };

                _context.ShipmentAttempts.Add(attempt);

                _context.PodEvidences.Add(new PodEvidence
                {
                    ShipmentAttempt = attempt,
                    PhotoUrl = "signature.png", // Mocked for now
                    OtpVerified = true,
                    Notes = comments
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
