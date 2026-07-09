using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetProductsAsync();
        Task<IEnumerable<Product>> GetProductsByMerchantIdAsync(int merchantId);
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product?> GetProductForDeleteAsync(int id);
        Task CreateProductAsync(Product product, Dictionary<int, int> warehouseQuantities);
        Task UpdateProductAsync(Product product, Dictionary<int, int> warehouseQuantities);
        Task DeleteProductAsync(int id);
        bool ProductExists(int id);

        Task<IEnumerable<Merchant>> GetMerchantsAsync();
        Task<IEnumerable<Warehouse>> GetWarehousesAsync();
        Task<IEnumerable<ProductWarehouse>> GetAllProductWarehousesAsync();
    }
}
