using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;
using FastDrop.MVC.ViewModels;
using System.Security.Claims;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Merchant")]
    public class MerchantPortalController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly IInvoiceService _invoiceService;
        private readonly IZoneService _zoneService;
        private readonly IProductService _productService;

        public MerchantPortalController(
            IShipmentService shipmentService, 
            IInvoiceService invoiceService, 
            IZoneService zoneService,
            IProductService productService)
        {
            _shipmentService = shipmentService;
            _invoiceService = invoiceService;
            _zoneService = zoneService;
            _productService = productService;
        }

        private int GetMerchantId()
        {
            var claim = User.FindFirst("MerchantId");
            if (claim != null && int.TryParse(claim.Value, out int merchantId))
            {
                return merchantId;
            }
            return 0;
        }

        // GET: MerchantPortal/Index
        public async Task<IActionResult> Index()
        {
            int merchantId = GetMerchantId();
            if (merchantId == 0) return RedirectToAction("Login", "Account");

            var shipments = await _shipmentService.GetShipmentsAsync("Merchant", merchantId, null);
            var invoices = await _invoiceService.GetInvoicesByMerchantIdAsync(merchantId);

            ViewBag.TotalShipments = shipments.Count();
            ViewBag.TotalCodAmount = shipments.Sum(s => s.CodAmount);
            ViewBag.PendingInvoicesCount = invoices.Count(i => i.PaymentStatus.ToLower() != "paid");

            return View();
        }

        // GET: MerchantPortal/Shipments
        public async Task<IActionResult> Shipments()
        {
            int merchantId = GetMerchantId();
            var shipments = await _shipmentService.GetShipmentsAsync("Merchant", merchantId, null);
            return View(shipments);
        }

        // GET: MerchantPortal/AddShipment
        public async Task<IActionResult> AddShipment()
        {
            int merchantId = GetMerchantId();
            ViewBag.Zones = await _zoneService.GetZonesAsync();
            var products = await _productService.GetProductsByMerchantIdAsync(merchantId);
            
            var vm = new AddShipmentViewModel
            {
                SelectedProducts = products.Select(p => new SelectedProduct
                {
                    ProductId = p.ProductId,
                    ProductName = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    IsSelected = false,
                    Quantity = 1
                }).ToList()
            };
            
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ShipmentDetails(int id)
        {
            int merchantId = GetMerchantId();
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null || shipment.MerchantId != merchantId)
            {
                return NotFound();
            }

            return View(shipment);
        }

        // POST: MerchantPortal/AddShipment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShipment(AddShipmentViewModel vm)
        {
            int merchantId = GetMerchantId();
            
            if (ModelState.IsValid)
            {
                decimal totalProductPrice = 0;
                
                // Add selected products to the shipment
                foreach (var sp in vm.SelectedProducts.Where(p => p.IsSelected))
                {
                    vm.Shipment.ShipmentProducts.Add(new ShipmentProduct
                    {
                        ProductId = sp.ProductId,
                        Quantity = sp.Quantity
                    });
                    
                    totalProductPrice += sp.Price * sp.Quantity;
                }

                if (vm.Shipment.Address != null && vm.Shipment.Address.ZoneId.HasValue)
                {
                    var zone = await _zoneService.GetZoneByIdAsync(vm.Shipment.Address.ZoneId.Value);
                    if (zone != null)
                    {
                        // New formula: Zone COD + Total Products Price
                        vm.Shipment.CodAmount = zone.CodAmount + totalProductPrice;
                    }
                }
                
                await _shipmentService.CreateShipmentAsync(vm.Shipment, merchantId);
                return RedirectToAction(nameof(Shipments));
            }

            ViewBag.Zones = await _zoneService.GetZonesAsync();
            return View(vm);
        }

        // GET: MerchantPortal/EditShipment/5
        public async Task<IActionResult> EditShipment(int id)
        {
            int merchantId = GetMerchantId();
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            
            if (shipment == null || shipment.MerchantId != merchantId)
            {
                return NotFound();
            }

            ViewBag.Zones = await _zoneService.GetZonesAsync();
            return View(shipment);
        }

        // POST: MerchantPortal/EditShipment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShipment(int id, Shipment shipment)
        {
            int merchantId = GetMerchantId();
            if (id != shipment.ShipmentId || shipment.MerchantId != merchantId) return NotFound();

            if (ModelState.IsValid)
            {
                var existingShipment = await _shipmentService.GetShipmentByIdAsync(id);
                if (existingShipment == null) return NotFound();

                if (shipment.Address != null && shipment.Address.ZoneId.HasValue)
                {
                    existingShipment.Address.Street = shipment.Address.Street;
                    existingShipment.Address.District = shipment.Address.District;
                    existingShipment.Address.ZoneId = shipment.Address.ZoneId;

                    // Note: Recalculating COD on edit is skipped for simplicity unless requested
                }

                existingShipment.WeightKg = shipment.WeightKg;
                
                await _shipmentService.UpdateShipmentAsync(existingShipment);
                return RedirectToAction(nameof(Shipments));
            }
            
            ViewBag.Zones = await _zoneService.GetZonesAsync();
            return View(shipment);
        }

        // GET: MerchantPortal/DeleteShipment/5
        public async Task<IActionResult> DeleteShipment(int id)
        {
            int merchantId = GetMerchantId();
            var shipment = await _shipmentService.GetShipmentForDeleteAsync(id);
            
            if (shipment == null || shipment.MerchantId != merchantId)
            {
                return NotFound();
            }

            return View(shipment);
        }

        // POST: MerchantPortal/DeleteShipment/5
        [HttpPost, ActionName("DeleteShipment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShipmentConfirmed(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment != null && shipment.MerchantId == GetMerchantId())
            {
                await _shipmentService.DeleteShipmentAsync(id);
            }
            return RedirectToAction(nameof(Shipments));
        }

        // GET: MerchantPortal/Invoices
        public async Task<IActionResult> Invoices()
        {
            int merchantId = GetMerchantId();
            var invoices = await _invoiceService.GetInvoicesByMerchantIdAsync(merchantId);
            return View(invoices);
        }

        // GET: MerchantPortal/InvoiceDetails/5
        public async Task<IActionResult> InvoiceDetails(int id)
        {
            int merchantId = GetMerchantId();
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);

            if (invoice == null || invoice.MerchantId != merchantId)
            {
                return NotFound();
            }

            var shipments = await _invoiceService.GetShipmentsForInvoiceAsync(id);
            ViewBag.Shipments = shipments;

            return View(invoice);
        }

    }
}
