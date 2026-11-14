using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;

namespace FastDrop.MVC.Controllers
{
    [Authorize]
    public class ShipmentsController : Controller
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentsController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        // GET: Shipments
        public async Task<IActionResult> Index()
        {
            string role = User.IsInRole("Merchant") ? "Merchant" : User.IsInRole("Driver") ? "Driver" : "Employee";
            
            int? merchantId = null;
            if (role == "Merchant")
            {
                var merchantIdStr = User.Claims.FirstOrDefault(c => c.Type == "MerchantId")?.Value;
                if (int.TryParse(merchantIdStr, out int mId)) merchantId = mId;
            }

            int? driverId = null;
            if (role == "Driver")
            {
                var driverIdStr = User.Claims.FirstOrDefault(c => c.Type == "DriverId")?.Value;
                if (int.TryParse(driverIdStr, out int dId)) driverId = dId;
            }

            var shipments = await _shipmentService.GetShipmentsAsync(role, merchantId, driverId);
            return View(shipments);
        }

        // GET: Shipments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shipment = await _shipmentService.GetShipmentByIdAsync(id.Value);
            if (shipment == null)
            {
                return NotFound();
            }

            return View(shipment);
        }

        [Authorize(Roles = "SuperAdmin,Dispatcher,Employee,Merchant")]
        public async Task<IActionResult> Create()
        {
            ViewData["AddressId"] = new SelectList(await _shipmentService.GetAddressesAsync(), "AddressId", "AddressId");
            if (!User.IsInRole("Merchant"))
            {
                ViewData["MerchantId"] = new SelectList(await _shipmentService.GetMerchantsAsync(), "MerchantId", "ShopName");
            }
            ViewData["RouteId"] = new SelectList(await _shipmentService.GetRoutesAsync(), "RouteId", "RouteName");
            ViewData["WarehouseId"] = new SelectList(await _shipmentService.GetWarehousesAsync(), "WarehouseId", "WarehouseName");
            return View();
        }

        // POST: Shipments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Dispatcher,Employee,Merchant")]
        public async Task<IActionResult> Create([Bind("ShipmentId,CreationDate,CodAmount,WeightKg,WarehouseId,MerchantId,RouteId,AddressId")] Shipment shipment)
        {
            int? merchantId = null;
            if (User.IsInRole("Merchant"))
            {
                var merchantIdStr = User.Claims.FirstOrDefault(c => c.Type == "MerchantId")?.Value;
                if (int.TryParse(merchantIdStr, out int mId))
                {
                    merchantId = mId;
                }
                // Clear validation state for MerchantId since we manually assign it
                ModelState.Remove("MerchantId");
            }

            if (ModelState.IsValid)
            {
                await _shipmentService.CreateShipmentAsync(shipment, merchantId);
                return RedirectToAction(nameof(Index));
            }
            ViewData["AddressId"] = new SelectList(await _shipmentService.GetAddressesAsync(), "AddressId", "AddressId", shipment.AddressId);
            if (!User.IsInRole("Merchant"))
            {
                ViewData["MerchantId"] = new SelectList(await _shipmentService.GetMerchantsAsync(), "MerchantId", "ShopName", shipment.MerchantId);
            }
            ViewData["RouteId"] = new SelectList(await _shipmentService.GetRoutesAsync(), "RouteId", "RouteName", shipment.RouteId);
            ViewData["WarehouseId"] = new SelectList(await _shipmentService.GetWarehousesAsync(), "WarehouseId", "WarehouseName", shipment.WarehouseId);
            return View(shipment);
        }

        [Authorize(Roles = "SuperAdmin,Dispatcher,Employee")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shipment = await _shipmentService.GetShipmentByIdAsync(id.Value);
            if (shipment == null)
            {
                return NotFound();
            }
            ViewData["AddressId"] = new SelectList(await _shipmentService.GetAddressesAsync(), "AddressId", "AddressId", shipment.AddressId);
            ViewData["MerchantId"] = new SelectList(await _shipmentService.GetMerchantsAsync(), "MerchantId", "ShopName", shipment.MerchantId);
            ViewData["RouteId"] = new SelectList(await _shipmentService.GetRoutesAsync(), "RouteId", "RouteName", shipment.RouteId);
            ViewData["WarehouseId"] = new SelectList(await _shipmentService.GetWarehousesAsync(), "WarehouseId", "WarehouseName", shipment.WarehouseId);
            return View(shipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Dispatcher,Employee")]
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
                    await _shipmentService.UpdateShipmentAsync(shipment);
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
            ViewData["AddressId"] = new SelectList(await _shipmentService.GetAddressesAsync(), "AddressId", "AddressId", shipment.AddressId);
            ViewData["MerchantId"] = new SelectList(await _shipmentService.GetMerchantsAsync(), "MerchantId", "ShopName", shipment.MerchantId);
            ViewData["RouteId"] = new SelectList(await _shipmentService.GetRoutesAsync(), "RouteId", "RouteName", shipment.RouteId);
            ViewData["WarehouseId"] = new SelectList(await _shipmentService.GetWarehousesAsync(), "WarehouseId", "WarehouseName", shipment.WarehouseId);
            return View(shipment);
        }

        [Authorize(Roles = "SuperAdmin,Dispatcher,Employee")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var shipment = await _shipmentService.GetShipmentForDeleteAsync(id.Value);
            if (shipment == null)
            {
                return NotFound();
            }

            return View(shipment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Dispatcher,Employee")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _shipmentService.DeleteShipmentAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private bool ShipmentExists(int id)
        {
            return _shipmentService.ShipmentExists(id);
        }

        // POST: Shipments/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Dispatcher,Driver,Employee")]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? comments = null)
        {
            string role = User.IsInRole("Driver") ? "Driver" : "Employee";
            int? driverId = null;
            
            if (role == "Driver")
            {
                var driverIdStr = User.Claims.FirstOrDefault(c => c.Type == "DriverId")?.Value;
                if (int.TryParse(driverIdStr, out int dId))
                {
                    driverId = dId;
                }
            }

            var result = await _shipmentService.UpdateStatusAsync(id, status, comments, role, driverId);

            if (!result.IsSuccess && !result.IsForbidden)
            {
                return NotFound();
            }
            
            if (result.IsForbidden)
            {
                return Forbid();
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
