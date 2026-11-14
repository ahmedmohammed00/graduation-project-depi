using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IWarehouseService
    {
        Task<IEnumerable<Warehouse>> GetWarehousesAsync();
        Task<Warehouse?> GetWarehouseByIdAsync(int id);
        Task<Warehouse?> GetWarehouseForDeleteAsync(int id);
        Task CreateWarehouseAsync(Warehouse warehouse);
        Task UpdateWarehouseAsync(Warehouse warehouse);
        Task DeleteWarehouseAsync(int id);
        bool WarehouseExists(int id);

        Task<IEnumerable<Address>> GetAddressesAsync();
        Task<Address?> GetAddressByIdAsync(int id);
        Task CreateAddressAsync(Address address);
        Task UpdateAddressAsync(Address address);
        Task DeleteAddressAsync(int id);
        
        Task<IEnumerable<ProductWarehouse>> GetProductsForWarehouseAsync(int warehouseId);
        Task AssignProductsToWarehouseAsync(int warehouseId, IEnumerable<int> productIds);
    }
}
