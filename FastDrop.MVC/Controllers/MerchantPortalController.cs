using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FastDrop.MVC.Models;
using FastDrop.MVC.Services;
using System.Security.Claims;

namespace FastDrop.MVC.Controllers
{
    [Authorize(Roles = "Merchant")]
    public class MerchantPortalController : Controller
    {
        private readonly IShipmentService _shipmentService;
        private readonly IInvoiceService _invoiceService;

        public MerchantPortalController(IShipmentService shipmentService, IInvoiceService invoiceService)
        {
            _shipmentService = shipmentService;
            _invoiceService = invoiceService;
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
            ViewBag.Addresses = await _shipmentService.GetAddressesAsync();
            return View(new Shipment());
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
        public async Task<IActionResult> AddShipment(Shipment shipment)
        {
            int merchantId = GetMerchantId();
            
            if (ModelState.IsValid)
            {
                await _shipmentService.CreateShipmentAsync(shipment, merchantId);
                return RedirectToAction(nameof(Shipments));
            }

            ViewBag.Addresses = await _shipmentService.GetAddressesAsync();
            return View(shipment);
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
