using System;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Driver")]
    public class DriverPortalController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly IRouteService _routeService;
        private readonly IPerformanceCalculationService _performanceService;
        private readonly IDriverPaymentService _driverPaymentService;

        public DriverPortalController(IShipmentService shipmentService, IRouteService routeService, IPerformanceCalculationService performanceService, IDriverPaymentService driverPaymentService)
        {
            _shipmentService = shipmentService;
            _routeService = routeService;
            _performanceService = performanceService;
            _driverPaymentService = driverPaymentService;
        }

        private int GetDriverId()
        {
            var driverIdClaim = User.FindFirst("DriverId");
            if (driverIdClaim != null && int.TryParse(driverIdClaim.Value, out int id))
            {
                return id;
            }
            return 0;
        }

        public async Task<IActionResult> Index()
        {
            int driverId = GetDriverId();
            if (driverId == 0) return RedirectToAction("Login", "Account");

            var routes = await _routeService.GetRoutesByDriverIdAsync(driverId);
            var shipments = await _shipmentService.GetShipmentsAsync("Driver", null, driverId);

            ViewBag.ActiveRoutesCount = routes.Count(r => r.Status.Equals("active", StringComparison.OrdinalIgnoreCase));
            ViewBag.TotalShipments = shipments.Count();
            ViewBag.DeliveredShipments = shipments.Count(s => 
                s.ShipmentStatuses.OrderByDescending(st => st.UpdatedAt).FirstOrDefault()?.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) == true);
            
            ViewBag.PendingShipments = ViewBag.TotalShipments - ViewBag.DeliveredShipments;

            // Calculate Performance Score
            var performance = await _performanceService.CalculateDriverPerformanceAsync(driverId);
            ViewBag.DriverPerformance = performance;

            return View();
        }

        public async Task<IActionResult> Routes()
        {
            int driverId = GetDriverId();
            var routes = await _routeService.GetRoutesByDriverIdAsync(driverId);
            return View(routes);
        }

        public async Task<IActionResult> Shipments()
        {
            int driverId = GetDriverId();
            var shipments = await _shipmentService.GetShipmentsAsync("Driver", null, driverId);
            return View(shipments);
        }

        public async Task<IActionResult> Payments()
        {
            int driverId = GetDriverId();
            var payments = await _driverPaymentService.GetDriverPaymentsAsync();
            var myPayments = payments.Where(p => p.DriverId == driverId).OrderByDescending(p => p.CreatedAt).ToList();
            
            ViewBag.TotalEarnings = myPayments.Sum(p => p.TotalPayout ?? 0);
            ViewBag.PendingEarnings = myPayments.Where(p => p.PaymentStatus == "Pending").Sum(p => p.TotalPayout ?? 0);
            ViewBag.PaidEarnings = myPayments.Where(p => p.PaymentStatus == "Paid").Sum(p => p.TotalPayout ?? 0);
            
            return View(myPayments);
        }

        [HttpGet]
        public async Task<IActionResult> UpdateStatus(int id)
        {
            var shipment = await _shipmentService.GetVisibleShipmentByIdAsync(id, User);
            if (shipment == null) return NotFound();

            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPod(int id, string otpCode, string notes)
        {
            int driverId = GetDriverId();
            var result = await _shipmentService.SubmitPodAsync(id, driverId, otpCode, notes);

            if (!result.IsSuccess)
            {
                TempData["ErrorMessage"] = result.Message;
                return RedirectToAction("UpdateStatus", new { id = id });
            }

            TempData["SuccessMessage"] = "Shipment delivered successfully!";
            return RedirectToAction("Shipments");
        }
    }
}
