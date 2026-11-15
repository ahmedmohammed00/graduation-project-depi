using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FastDrop.MVC.Services;
using FastDrop.MVC.Models;
using FastDrop.MVC.ViewModels;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminPortalController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDriverPaymentService _driverPaymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly IMerchantService _merchantService;
        private readonly IWarehouseService _warehouseService;
        private readonly IZoneService _zoneService;
        private readonly IProductService _productService;
        private readonly IShipmentService _shipmentService;

        public AdminPortalController(
            IEmployeeService employeeService,
            IDriverPaymentService driverPaymentService,
            IInvoiceService invoiceService,
            IMerchantService merchantService,
            IWarehouseService warehouseService,
            IZoneService zoneService,
            IProductService productService,
            IShipmentService shipmentService)
        {
            _employeeService = employeeService;
            _driverPaymentService = driverPaymentService;
            _invoiceService = invoiceService;
            _merchantService = merchantService;
            _warehouseService = warehouseService;
            _zoneService = zoneService;
            _productService = productService;
            _shipmentService = shipmentService;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            var invoices = await _invoiceService.GetInvoicesAsync();
            var payments = await _driverPaymentService.GetDriverPaymentsAsync();
            var merchants = await _merchantService.GetMerchantsAsync();

            return View(new AdminDashboardViewModel
            {
                EmployeeCount = employees.Count(),
                MerchantCount = merchants.Count(),
                ActiveMerchantCount = merchants.Count(m => string.Equals(m.Status, "active", StringComparison.OrdinalIgnoreCase)),
                UnpaidInvoiceCount = invoices.Count(i => i.PaymentStatus != "Paid"),
                UnpaidInvoiceAmount = invoices.Where(i => i.PaymentStatus != "Paid").Sum(i => i.AmountDue),
                PendingPaymentCount = payments.Count(p => p.PaymentStatus != "Paid"),
                PendingPaymentAmount = payments.Where(p => p.PaymentStatus != "Paid").Sum(p => p.TotalPayout ?? 0m),
                Merchants = merchants.OrderBy(m => m.ShopName).Take(5)
                    .Select(m => new DashboardMerchant(m.MerchantId, m.ShopName, m.ContactName, m.Email, m.Status)).ToList()
            });
        }

        // --- 1. Employees & Roles ---
        public async Task<IActionResult> Employees()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            return View(employees);
        }

        [HttpGet]
        public async Task<IActionResult> EditEmployee(int id)
        {
            var employee = await _employeeService.GetEmployeeByIdAsync(id);
            if (employee == null) return NotFound();

            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _employeeService.GetRolesAsync(), "RoleId", "RoleName", employee.RoleId);
            return View(new EmployeeRoleEditViewModel
            {
                EmployeeId = employee.EmployeeId, Name = employee.Name,
                Email = employee.Email, RoleId = employee.RoleId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(int id, EmployeeRoleEditViewModel employee)
        {
            if (id != employee.EmployeeId) return NotFound();

            if (ModelState.IsValid)
            {
                if (!await _employeeService.UpdateEmployeeAsync(employee)) return NotFound();
                TempData["SuccessMessage"] = "Employee role updated successfully.";
                return RedirectToAction(nameof(Employees));
            }
            
            var existing = await _employeeService.GetEmployeeByIdAsync(id);
            if (existing == null) return NotFound();
            employee.Name = existing.Name;
            employee.Email = existing.Email;
            ViewBag.Roles = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(await _employeeService.GetRolesAsync(), "RoleId", "RoleName", employee.RoleId);
            return View(employee);
        }

        // --- 2. Merchants ---
        public async Task<IActionResult> Merchants()
        {
            var merchants = await _merchantService.GetMerchantsAsync();
            return View(merchants);
        }

        public async Task<IActionResult> MerchantDetails(int id)
        {
            var merchant = await _merchantService.GetMerchantByIdAsync(id);
            if (merchant == null)
            {
                return NotFound();
            }
            return View(merchant);
        }

        public async Task<IActionResult> ShipmentDetails(int id)
        {
            var shipment = await _shipmentService.GetVisibleShipmentByIdAsync(id, User);
            if (shipment == null)
            {
                return NotFound();
            }
            return View(shipment);
        }

        // --- 3. Invoices ---
        public async Task<IActionResult> Invoices()
        {
            var invoices = await _invoiceService.GetInvoicesAsync();
            ViewBag.Merchants = await _merchantService.GetMerchantsAsync();
            return View(invoices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvoice(int merchantId, DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
            {
                TempData["ErrorMessage"] = "Start date cannot be after end date.";
                return RedirectToAction(nameof(Invoices));
            }

            var result = await _invoiceService.GenerateInvoiceAsync(merchantId, startDate, endDate);
            
            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = $"Successfully generated invoice for {result.Invoice?.AmountDue:C}.";
            }

            return RedirectToAction(nameof(Invoices));
        }

        // --- 4. Driver Payments ---
        public async Task<IActionResult> DriverPayments()
        {
            var payments = await _driverPaymentService.GetDriverPaymentsAsync();
            return View(payments.OrderByDescending(p => p.CreatedAt));
        }

        [HttpPost]
        public async Task<IActionResult> MarkPaymentPaid(int id)
        {
            var payment = await _driverPaymentService.GetDriverPaymentByIdAsync(id);
            if (payment != null)
            {
                payment.PaymentStatus = "Paid";
                await _driverPaymentService.UpdateDriverPaymentAsync(payment);
                TempData["SuccessMessage"] = "Payment marked as paid.";
            }
            return RedirectToAction(nameof(DriverPayments));
        }

        // --- 5. System: Warehouses, Inventory & Zones ---
        public async Task<IActionResult> Warehouses()
        {
            var warehouses = await _warehouseService.GetWarehousesAsync();
            return View(warehouses);
        }

        public async Task<IActionResult> Inventory()
        {
            var inventoryItems = await _productService.GetAllProductWarehousesAsync();
            return View(inventoryItems);
        }

        public async Task<IActionResult> Zones()
        {
            var zones = await _zoneService.GetZonesAsync();
            return View(zones);
        }

        [HttpGet]
        public IActionResult CreateZone()
        {
            return View(new Zone { ActiveStatus = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateZone(Zone zone)
        {
            if (ModelState.IsValid)
            {
                await _zoneService.CreateZoneAsync(zone);
                TempData["SuccessMessage"] = "Zone created successfully.";
                return RedirectToAction(nameof(Zones));
            }
            return View(zone);
        }

        public async Task<IActionResult> EditZone(int id)
        {
            var zone = await _zoneService.GetZoneByIdAsync(id);
            if (zone == null) return NotFound();
            return View(zone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditZone(int id, Zone zone)
        {
            if (id != zone.ZoneId) return NotFound();
            if (ModelState.IsValid)
            {
                await _zoneService.UpdateZoneAsync(zone);
                TempData["SuccessMessage"] = "Zone updated successfully.";
                return RedirectToAction(nameof(Zones));
            }
            return View(zone);
        }

        public async Task<IActionResult> DeleteZone(int id)
        {
            var zone = await _zoneService.GetZoneByIdAsync(id);
            if (zone == null) return NotFound();
            return View(zone);
        }

        [HttpPost, ActionName("DeleteZone")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteZoneConfirmed(int id)
        {
            await _zoneService.DeleteZoneAsync(id);
            TempData["SuccessMessage"] = "Zone deleted successfully.";
            return RedirectToAction(nameof(Zones));
        }

        public async Task<IActionResult> WarehouseDetails(int id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null) return NotFound();

            var assignedProducts = await _warehouseService.GetProductsForWarehouseAsync(id);
            ViewBag.AssignedProducts = assignedProducts;

            return View(warehouse);
        }

        [HttpGet]
        public async Task<IActionResult> AssignProducts(int id)
        {
            var warehouse = await _warehouseService.GetWarehouseByIdAsync(id);
            if (warehouse == null) return NotFound();

            var allProducts = await _productService.GetProductsAsync();
            var assignedProducts = await _warehouseService.GetProductsForWarehouseAsync(id);
            
            ViewBag.WarehouseName = warehouse.WarehouseName;
            ViewBag.AllProducts = allProducts;
            ViewBag.AssignedProductIds = assignedProducts.Select(p => p.ProductId).ToList();

            return View(new { WarehouseId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignProducts(int warehouseId, List<int> productIds)
        {
            await _warehouseService.AssignProductsToWarehouseAsync(warehouseId, productIds);
            TempData["SuccessMessage"] = "Products successfully assigned to the warehouse.";
            return RedirectToAction(nameof(WarehouseDetails), new { id = warehouseId });
        }
    }
}
