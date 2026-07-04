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
        Task<Invoice?> GenerateInvoiceAsync(int merchantId, int shipmentCount);
        Task CreateInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int id);
        bool InvoiceExists(int id);

        Task<IEnumerable<Merchant>> GetMerchantsAsync();
        Task<IEnumerable<Shipment>> GetShipmentsAsync();
    }
}
