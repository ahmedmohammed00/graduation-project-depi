using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    public class DriversController : Controller
    {
        private readonly IDriverService _driverService;

        public DriversController(IDriverService driverService)
        {
            _driverService = driverService;
        }

        // GET: Drivers
        public async Task<IActionResult> Index()
        {
            var drivers = await _driverService.GetDriversAsync();
            return View(drivers);
        }

        // GET: Drivers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _driverService.GetDriverByIdAsync(id.Value);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // GET: Drivers/Create
        public async Task<IActionResult> Create()
        {
            ViewData["AddressId"] = new SelectList(await _driverService.GetAddressesAsync(), "AddressId", "AddressId");
            ViewData["EmployeeId"] = new SelectList(await _driverService.GetEmployeesAsync(), "EmployeeId", "EmployeeId");
            ViewData["ZoneId"] = new SelectList(await _driverService.GetZonesAsync(), "ZoneId", "ZoneId");
            return View();
        }

        // POST: Drivers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DriverId,Name,PhoneNumber,LicenseNumber,LicenseExpiryDate,CurrentStatus,ZoneId,AddressId,EmployeeId")] Driver driver)
        {
            if (ModelState.IsValid)
            {
                await _driverService.CreateDriverAsync(driver);
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(await _driverService.GetAddressesAsync(), "AddressId", "AddressId", driver.AddressId);
            ViewData["EmployeeId"] = new SelectList(await _driverService.GetEmployeesAsync(), "EmployeeId", "EmployeeId", driver.EmployeeId);
            ViewData["ZoneId"] = new SelectList(await _driverService.GetZonesAsync(), "ZoneId", "ZoneId", driver.ZoneId);
            return View(driver);
        }

        // GET: Drivers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var driver = await _driverService.GetDriverByIdAsync(id.Value);
            if (driver == null) return NotFound();
            
            ViewData["AddressId"] = new SelectList(await _driverService.GetAddressesAsync(), "AddressId", "AddressId", driver.AddressId);
            ViewData["EmployeeId"] = new SelectList(await _driverService.GetEmployeesAsync(), "EmployeeId", "EmployeeId", driver.EmployeeId);
            ViewData["ZoneId"] = new SelectList(await _driverService.GetZonesAsync(), "ZoneId", "ZoneId", driver.ZoneId);
            
            var vehicles = await _driverService.GetVehiclesAsync();
            var currentVehicleId = await _driverService.GetDriverVehicleIdAsync(driver.DriverId);
            ViewData["VehicleId"] = new SelectList(vehicles, "VehicleId", "LicensePlate", currentVehicleId);

            return View(driver);
        }

        // POST: Drivers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DriverId,Name,PhoneNumber,LicenseNumber,LicenseExpiryDate,CurrentStatus,ZoneId,AddressId,EmployeeId")] Driver driver, int? VehicleId)
        {
            if (id != driver.DriverId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _driverService.UpdateDriverAsync(driver);
                    if (VehicleId.HasValue)
                    {
                        await _driverService.AssignVehicleToDriverAsync(driver.DriverId, VehicleId.Value);
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DriverExists(driver.DriverId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(await _driverService.GetAddressesAsync(), "AddressId", "AddressId", driver.AddressId);
            ViewData["EmployeeId"] = new SelectList(await _driverService.GetEmployeesAsync(), "EmployeeId", "EmployeeId", driver.EmployeeId);
            ViewData["ZoneId"] = new SelectList(await _driverService.GetZonesAsync(), "ZoneId", "ZoneId", driver.ZoneId);
            ViewData["VehicleId"] = new SelectList(await _driverService.GetVehiclesAsync(), "VehicleId", "LicensePlate", VehicleId);
            return View(driver);
        }

        // GET: Drivers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var driver = await _driverService.GetDriverForDeleteAsync(id.Value);
            if (driver == null)
            {
                return NotFound();
            }

            return View(driver);
        }

        // POST: Drivers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _driverService.DeleteDriverAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool DriverExists(int id)
        {
            return _driverService.DriverExists(id);
        }
    }
}
