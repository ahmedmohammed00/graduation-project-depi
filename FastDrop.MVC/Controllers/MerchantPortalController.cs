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
using FastDrop.MVC.Security;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Merchant")]
    public class MerchantPortalController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly IInvoiceService _invoiceService;
        private readonly IZoneService _zoneService;
        private readonly IProductService _productService;
        private readonly IMerchantOperationsService _merchantOperations;

        public MerchantPortalController(
            IShipmentService shipmentService, 
            IInvoiceService invoiceService, 
            IZoneService zoneService,
            IProductService productService,
            IMerchantOperationsService merchantOperations)
        {
            _shipmentService = shipmentService;
            _invoiceService = invoiceService;
            _zoneService = zoneService;
            _productService = productService;
            _merchantOperations = merchantOperations;
        }

        private int GetMerchantId() => MerchantIdentity.GetId(User) ?? 0;

        private async Task PopulateShipmentFormAsync(AddShipmentViewModel model)
        {
            ViewBag.Zones = (await _zoneService.GetZonesAsync()).Where(z => z.ActiveStatus);
            var submitted = (model.SelectedProducts ?? new()).GroupBy(p => p.ProductId)
                .ToDictionary(g => g.Key, g => g.First());
            var products = await _productService.GetProductsByMerchantIdAsync(GetMerchantId());
            model.SelectedProducts = products.OrderBy(p => p.ProductId).Select(p => new SelectedProduct
            {
                ProductId = p.ProductId, ProductName = p.Name, Price = p.Price, StockQuantity = p.StockQuantity,
                IsSelected = submitted.TryGetValue(p.ProductId, out var selected) && selected.IsSelected,
                Quantity = submitted.TryGetValue(p.ProductId, out var item) ? item.Quantity : 1
            }).ToList();
            // Rebuilt row indices and catalog fields must come from the server, including after a forged POST.
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("SelectedProducts[", StringComparison.Ordinal)).ToList())
                ModelState.Remove(key);
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
            if (GetMerchantId() == 0) return NotFound();
            var model = new AddShipmentViewModel();
            await PopulateShipmentFormAsync(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ShipmentDetails(int id)
        {
            int merchantId = GetMerchantId();
            var shipment = await _shipmentService.GetVisibleShipmentByIdAsync(id, User);
            if (shipment == null || shipment.MerchantId != merchantId)
            {
                return NotFound();
            }

            return View(shipment);
        }

        // POST: MerchantPortal/AddShipment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddShipment(AddShipmentViewModel model)
        {
            if (GetMerchantId() == 0) return NotFound();
            var result = ModelState.IsValid
                ? await _merchantOperations.CreateShipmentAsync(model, User)
                : MerchantWriteResult.Invalid("Review the delivery details and selected product quantities.");
            if (result.StatusCode == 404) return NotFound();
            if (result.StatusCode == 200) return RedirectToAction(nameof(Shipments));
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = result.StatusCode;
            model.Shipment ??= new();
            await PopulateShipmentFormAsync(model);
            return View(model);
        }

        // GET: MerchantPortal/EditShipment/5
        public async Task<IActionResult> EditShipment(int id)
        {
            var model = await _merchantOperations.GetShipmentEditAsync(id, User);
            if (model == null) return NotFound();
            ViewBag.Zones = (await _zoneService.GetZonesAsync()).Where(z => z.ActiveStatus);
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditShipment(int id, MerchantShipmentEditViewModel model)
        {
            if (await _merchantOperations.GetShipmentEditAsync(id, User) == null) return NotFound();
            var result = ModelState.IsValid
                ? await _merchantOperations.EditShipmentAsync(id, model, User)
                : MerchantWriteResult.Invalid("Review the delivery address, zone and weight.");
            if (result.StatusCode == 404) return NotFound();
            if (result.StatusCode == 200) return RedirectToAction(nameof(Shipments));
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = result.StatusCode;
            model.ShipmentId = id;
            ViewBag.Zones = (await _zoneService.GetZonesAsync()).Where(z => z.ActiveStatus);
            return View(model);
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

        // GET: MerchantPortal/Products
        public async Task<IActionResult> Products()
        {
            int merchantId = GetMerchantId();
            var products = await _productService.GetProductsByMerchantIdAsync(merchantId);
            return View(products);
        }

        // GET: MerchantPortal/AddProduct
        public IActionResult AddProduct()
        {
            return View();
        }

        // POST: MerchantPortal/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct([Bind("Name,Description,Price,StockQuantity")] Product product)
        {
            if (ModelState.IsValid)
            {
                product.MerchantId = GetMerchantId();
                await _productService.CreateMerchantProductAsync(product);
                return RedirectToAction(nameof(Products));
            }
            return View(product);
        }

        // GET: MerchantPortal/EditProduct/5
        public async Task<IActionResult> EditProduct(int id)
        {
            var model = await _merchantOperations.GetProductEditAsync(id, User);
            return model == null ? NotFound() : View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(int id, MerchantProductEditViewModel model)
        {
            if (await _merchantOperations.GetProductEditAsync(id, User) == null) return NotFound();
            var result = ModelState.IsValid
                ? await _merchantOperations.EditProductAsync(id, model, User)
                : MerchantWriteResult.Invalid("Review the product details, price and stock.");
            if (result.StatusCode == 404) return NotFound();
            if (result.StatusCode == 200) return RedirectToAction(nameof(Products));
            ModelState.AddModelError(string.Empty, result.Error!);
            Response.StatusCode = result.StatusCode;
            model.ProductId = id;
            return View(model);
        }

        // GET: MerchantPortal/DeleteProduct/5
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productService.GetProductForDeleteAsync(id);
            if (product == null || product.MerchantId != GetMerchantId())
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: MerchantPortal/DeleteProduct/5
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductConfirmed(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product != null && product.MerchantId == GetMerchantId())
            {
                await _productService.DeleteProductAsync(id);
            }
            return RedirectToAction(nameof(Products));
        }
    }
}
