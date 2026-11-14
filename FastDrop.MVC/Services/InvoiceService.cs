using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly DmsDbContext _context;

        public InvoiceService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesAsync()
        {
            return await _context.Invoices.Include(i => i.Merchant).Include(i => i.Shipment).ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Merchant)
                .Include(i => i.Shipment)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);
        }

        public async Task<Invoice?> GetInvoiceForDeleteAsync(int id)
        {
            return await _context.Invoices
                .Include(i => i.Merchant)
                .Include(i => i.Shipment)
                .FirstOrDefaultAsync(m => m.InvoiceId == id);
        }

        public async Task<(bool IsSuccess, string Message, Invoice? Invoice)> GenerateInvoiceAsync(int merchantId, DateTime startDate, DateTime endDate)
        {
            // 1. Check for overlapping invoices
            var overlapExists = await _context.Invoices
                .Where(i => i.MerchantId == merchantId && i.PeriodStart <= endDate && i.PeriodEnd >= startDate)
                .AnyAsync();

            if (overlapExists)
            {
                return (false, "An invoice already exists for this merchant that overlaps with the requested date range.", null);
            }

            // 2. Find all Shipment Attempts in this period for the merchant
            // To do this, we need to join from ShipmentAttempt -> Shipment -> Address -> Zone
            var attempts = await _context.Shipments
                .Where(s => s.MerchantId == merchantId)
                .SelectMany(s => s.ShipmentAttempts)
                .Where(a => a.AttemptTime >= startDate && a.AttemptTime <= endDate)
                .Include(a => a.Shipment)
                    .ThenInclude(s => s.Address)
                        .ThenInclude(addr => addr.Zone)
                .ToListAsync();

            if (!attempts.Any())
            {
                return (false, "No shipment attempts found for this merchant in the specified date range.", null);
            }

            // 3. Calculate Amount Due based on DeliveryFee per attempt
            decimal totalAmount = 0m;
            foreach (var attempt in attempts)
            {
                var fee = attempt.Shipment?.Address?.Zone?.DeliveryFee ?? 0m;
                totalAmount += fee;
            }

            var invoice = new Invoice
            {
                MerchantId = merchantId,
                AmountDue = totalAmount,
                PaymentStatus = "Unpaid",
                PeriodStart = startDate,
                PeriodEnd = endDate
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return (true, "Invoice generated successfully.", invoice);
        }

        public async Task CreateInvoiceAsync(Invoice invoice)
        {
            _context.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _context.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public bool InvoiceExists(int id)
        {
            return _context.Invoices.Any(e => e.InvoiceId == id);
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByMerchantIdAsync(int merchantId)
        {
            return await _context.Invoices
                .Include(i => i.Shipment)
                .Where(i => i.MerchantId == merchantId)
                .OrderByDescending(i => i.PeriodEnd ?? i.PeriodStart)
                .ToListAsync();
        }

        public async Task<IEnumerable<Shipment>> GetShipmentsForInvoiceAsync(int invoiceId)
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);
            if (invoice == null) return new List<Shipment>();

            if (invoice.ShipmentId.HasValue)
            {
                return await _context.Shipments
                    .Where(s => s.ShipmentId == invoice.ShipmentId)
                    .ToListAsync();
            }
            else if (invoice.PeriodStart.HasValue && invoice.PeriodEnd.HasValue)
            {
                return await _context.Shipments
                    .Where(s => s.MerchantId == invoice.MerchantId && s.CreationDate >= invoice.PeriodStart && s.CreationDate <= invoice.PeriodEnd)
                    .ToListAsync();
            }

            return new List<Shipment>();
        }

        public async Task<IEnumerable<Merchant>> GetMerchantsAsync()
        {
            return await _context.Merchants.ToListAsync();
        }

        public async Task<IEnumerable<Shipment>> GetShipmentsAsync()
        {
            return await _context.Shipments.ToListAsync();
        }
    }
}
