using System.Threading.Tasks;
using FastDrop.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Dispatcher,SuperAdmin")]
    public class DispatcherPortalController : Controller
    {
        private readonly IDispatcherService _dispatcherService;

        public DispatcherPortalController(IDispatcherService dispatcherService)
        {
            _dispatcherService = dispatcherService;
        }

        public async Task<IActionResult> Index()
        {
            var unassignedShipments = await _dispatcherService.GetUnassignedShipmentsAsync();
            var availableDrivers = await _dispatcherService.GetAvailableDriversAsync();
            var activeRoutes = await _dispatcherService.GetActiveRoutesAsync();

            ViewBag.UnassignedShipments = unassignedShipments;
            ViewBag.AvailableDrivers = availableDrivers;
            ViewBag.ActiveRoutes = activeRoutes;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToDriver(int shipmentId, int driverId)
        {
            var result = await _dispatcherService.AssignShipmentToDriverDirectlyAsync(shipmentId, driverId);
            
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignToRoute(int shipmentId, int routeId)
        {
            var result = await _dispatcherService.AssignShipmentToRouteAsync(shipmentId, routeId);
            
            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SmartAssign()
        {
            var assignedCount = await _dispatcherService.SmartAssignShipmentsAsync();

            if (assignedCount > 0)
            {
                TempData["SuccessMessage"] = $"Smart Assign completed. {assignedCount} shipments were automatically dispatched.";
            }
            else
            {
                TempData["ErrorMessage"] = "Smart Assign could not find any matching drivers for the pending shipments.";
            }

            return RedirectToAction("Index");
        }
    }
}
