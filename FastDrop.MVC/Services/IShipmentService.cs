using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Services
{
    public interface IShipmentService
    {
        Task<IEnumerable<Shipment>> GetShipmentsAsync(string role, int? merchantId, int? driverId);
        Task<Shipment?> GetShipmentByIdAsync(int id);
        Task<Shipment?> GetShipmentForDeleteAsync(int id);
        Task CreateShipmentAsync(Shipment shipment, int? merchantId);
        Task UpdateShipmentAsync(Shipment shipment);
        Task DeleteShipmentAsync(int id);
        bool ShipmentExists(int id);
        Task<(bool IsSuccess, bool IsForbidden)> UpdateStatusAsync(int id, string status, string? comments, string role, int? driverId);

        // Lookup methods for dropdowns
        Task<IEnumerable<Address>> GetAddressesAsync();
        Task<IEnumerable<Merchant>> GetMerchantsAsync();
        Task<IEnumerable<Route>> GetRoutesAsync();
        Task<IEnumerable<Warehouse>> GetWarehousesAsync();
    }
}
