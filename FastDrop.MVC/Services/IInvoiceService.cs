using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IInvoiceService
    {
        Task<IEnumerable<Invoice>> GetInvoicesAsync();
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task<Invoice?> GetInvoiceForDeleteAsync(int id);
        Task<(bool IsSuccess, string Message, Invoice? Invoice)> GenerateInvoiceAsync(int merchantId, DateTime startDate, DateTime endDate);
        Task CreateInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int id);
        bool InvoiceExists(int id);
        Task<IEnumerable<Invoice>> GetInvoicesByMerchantIdAsync(int merchantId);
        Task<IEnumerable<Shipment>> GetShipmentsForInvoiceAsync(int invoiceId);

        Task<IEnumerable<Merchant>> GetMerchantsAsync();
        Task<IEnumerable<Shipment>> GetShipmentsAsync();
    }
}
