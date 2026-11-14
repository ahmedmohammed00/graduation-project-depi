using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IDispatcherService
    {
        Task<IEnumerable<Shipment>> GetUnassignedShipmentsAsync();
        Task<IEnumerable<Driver>> GetAvailableDriversAsync();
        Task<IEnumerable<FastDrop.MVC.Models.Route>> GetActiveRoutesAsync();
        Task<(bool IsSuccess, string Message)> AssignShipmentToDriverDirectlyAsync(int shipmentId, int driverId);
        Task<(bool IsSuccess, string Message)> AssignShipmentToRouteAsync(int shipmentId, int routeId);
        Task<int> SmartAssignShipmentsAsync();
    }
}
