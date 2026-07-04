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

        public async Task<Invoice?> GenerateInvoiceAsync(int merchantId, int shipmentCount)
        {
            var shipments = await _context.Shipments
                .Where(s => s.MerchantId == merchantId)
                .OrderByDescending(s => s.CreationDate)
                .Take(shipmentCount)
                .ToListAsync();

            if (!shipments.Any())
            {
                return null;
            }

            decimal totalAmount = shipments.Sum(s => s.CodAmount);

            var invoice = new Invoice
            {
                MerchantId = merchantId,
                AmountDue = totalAmount,
                PaymentStatus = "Unpaid",
                PeriodStart = shipments.Min(s => s.CreationDate),
                PeriodEnd = shipments.Max(s => s.CreationDate)
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return invoice;
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
