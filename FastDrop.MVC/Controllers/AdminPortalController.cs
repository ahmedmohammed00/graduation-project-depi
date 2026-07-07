using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FastDrop.MVC.Services;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Controllers
{
    // Normally [Authorize(Roles = "SuperAdmin")] but for testing we might just allow Employee
    [Authorize(Roles = "Employee")]
    public class AdminPortalController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly IDriverPaymentService _driverPaymentService;
        private readonly IInvoiceService _invoiceService;
        private readonly IMerchantService _merchantService;
        private readonly IWarehouseService _warehouseService;
        private readonly IZoneService _zoneService;

        public AdminPortalController(
            IEmployeeService employeeService,
            IDriverPaymentService driverPaymentService,
            IInvoiceService invoiceService,
            IMerchantService merchantService,
            IWarehouseService warehouseService,
            IZoneService zoneService)
        {
            _employeeService = employeeService;
            _driverPaymentService = driverPaymentService;
            _invoiceService = invoiceService;
            _merchantService = merchantService;
            _warehouseService = warehouseService;
            _zoneService = zoneService;
        }

        public async Task<IActionResult> Index()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            var invoices = await _invoiceService.GetInvoicesAsync();
            var payments = await _driverPaymentService.GetDriverPaymentsAsync();
            var merchants = await _merchantService.GetMerchantsAsync();

            ViewBag.EmployeeCount = employees.Count();
            ViewBag.UnpaidInvoicesCount = invoices.Count(i => i.PaymentStatus != "Paid");
            ViewBag.PendingPaymentsCount = payments.Count(p => p.PaymentStatus != "Paid");
            ViewBag.MerchantCount = merchants.Count();

            return View();
        }

        // --- 1. Employees & Roles ---
        public async Task<IActionResult> Employees()
        {
            var employees = await _employeeService.GetEmployeesAsync();
            return View(employees);
        }

        // --- 2. Merchants ---
        public async Task<IActionResult> Merchants()
        {
            var merchants = await _merchantService.GetMerchantsAsync();
            return View(merchants);
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

        // --- 5. System: Warehouses & Zones ---
        public async Task<IActionResult> Warehouses()
        {
            var warehouses = await _warehouseService.GetWarehousesAsync();
            return View(warehouses);
        }

        public async Task<IActionResult> Zones()
        {
            var zones = await _zoneService.GetZonesAsync();
            return View(zones);
        }
    }
}
