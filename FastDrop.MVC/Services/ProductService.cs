using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class ProductService : IProductService
    {
        private readonly DmsDbContext _context;

        public ProductService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Product>> GetProductsAsync()
        {
            return await _context.Products.Include(p => p.Merchant).AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByMerchantIdAsync(int merchantId)
        {
            return await _context.Products
                .Include(p => p.ProductWarehouses)
                    .ThenInclude(pw => pw.Warehouse)
                .Where(p => p.MerchantId == merchantId)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Merchant)
                .Include(p => p.ProductWarehouses)
                .FirstOrDefaultAsync(m => m.ProductId == id);
        }

        public async Task<Product?> GetProductForDeleteAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(m => m.ProductId == id);
        }

        public async Task CreateProductAsync(Product product, Dictionary<int, int> warehouseQuantities)
        {
            // Calculate total stock
            product.StockQuantity = warehouseQuantities.Values.Sum();

            _context.Add(product);
            await _context.SaveChangesAsync(); // Generates ProductId

            foreach (var wq in warehouseQuantities)
            {
                if (wq.Value > 0)
                {
                    _context.ProductWarehouses.Add(new ProductWarehouse
                    {
                        ProductId = product.ProductId,
                        WarehouseId = wq.Key,
                        Quantity = wq.Value
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task CreateMerchantProductAsync(Product product)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMerchantProductAsync(Product product)
        {
            // Only update basic properties to avoid wiping out warehouses if they exist
            var existing = await _context.Products.FindAsync(product.ProductId);
            if (existing != null)
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.StockQuantity = product.StockQuantity;
                _context.Update(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateProductAsync(Product product, Dictionary<int, int> warehouseQuantities)
        {
            product.StockQuantity = warehouseQuantities.Values.Sum();
            _context.Update(product);

            var existingMappings = await _context.ProductWarehouses
                .Where(pw => pw.ProductId == product.ProductId)
                .ToListAsync();

            // Remove mappings that are no longer in the dictionary or have <= 0 quantity
            var mappingsToRemove = existingMappings
                .Where(em => !warehouseQuantities.ContainsKey(em.WarehouseId) || warehouseQuantities[em.WarehouseId] <= 0)
                .ToList();
            _context.ProductWarehouses.RemoveRange(mappingsToRemove);

            // Update existing or add new
            foreach (var wq in warehouseQuantities.Where(wq => wq.Value > 0))
            {
                var existing = existingMappings.FirstOrDefault(em => em.WarehouseId == wq.Key);
                if (existing != null)
                {
                    existing.Quantity = wq.Value;
                    _context.ProductWarehouses.Update(existing);
                }
                else
                {
                    _context.ProductWarehouses.Add(new ProductWarehouse
                    {
                        ProductId = product.ProductId,
                        WarehouseId = wq.Key,
                        Quantity = wq.Value
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }

        public bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductId == id);
        }

        public async Task<IEnumerable<Merchant>> GetMerchantsAsync()
        {
            return await _context.Merchants.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Warehouse>> GetWarehousesAsync()
        {
            return await _context.Warehouses.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<ProductWarehouse>> GetAllProductWarehousesAsync()
        {
            return await _context.ProductWarehouses
                .Include(pw => pw.Product)
                    .ThenInclude(p => p.Merchant)
                .Include(pw => pw.Warehouse)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
