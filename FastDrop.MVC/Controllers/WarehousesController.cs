using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    public class WarehousesController : Controller
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        // GET: Warehouses
        public async Task<IActionResult> Index()
        {
            var warehouses = await _warehouseService.GetWarehousesAsync();
            return View(warehouses);
        }

        // GET: Warehouses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id.Value);
            if (warehouse == null)
            {
                return NotFound();
            }

            return View(warehouse);
        }

        // GET: Warehouses/Create
        public async Task<IActionResult> Create()
        {
            ViewData["AddressId"] = new SelectList(await _warehouseService.GetAddressesAsync(), "AddressId", "AddressId");
            return View();
        }

        // POST: Warehouses/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("WarehouseId,WarehouseName,AddressId")] Warehouse warehouse)
        {
            if (ModelState.IsValid)
            {
                await _warehouseService.CreateWarehouseAsync(warehouse);
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(await _warehouseService.GetAddressesAsync(), "AddressId", "AddressId", warehouse.AddressId);
            return View(warehouse);
        }

        // GET: Warehouses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id.Value);
            if (warehouse == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(await _warehouseService.GetAddressesAsync(), "AddressId", "AddressId", warehouse.AddressId);
            return View(warehouse);
        }

        // POST: Warehouses/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("WarehouseId,WarehouseName,AddressId")] Warehouse warehouse)
        {
            if (id != warehouse.WarehouseId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _warehouseService.UpdateWarehouseAsync(warehouse);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!WarehouseExists(warehouse.WarehouseId))
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
            ViewData["AddressId"] = new SelectList(await _warehouseService.GetAddressesAsync(), "AddressId", "AddressId", warehouse.AddressId);
            return View(warehouse);
        }

        // GET: Warehouses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var warehouse = await _warehouseService.GetWarehouseForDeleteAsync(id.Value);
            if (warehouse == null)
            {
                return NotFound();
            }

            return View(warehouse);
        }

        // POST: Warehouses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _warehouseService.DeleteWarehouseAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool WarehouseExists(int id)
        {
            return _warehouseService.WarehouseExists(id);
        }
    }
}
