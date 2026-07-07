using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IDispatcherService
    {
        Task<IEnumerable<Shipment>> GetUnassignedShipmentsAsync();
        Task<IEnumerable<Driver>> GetAvailableDriversAsync();
        Task<(bool IsSuccess, string Message)> AssignShipmentToDriverAsync(int shipmentId, int driverId);
        Task<int> SmartAssignShipmentsAsync();
    }
}
