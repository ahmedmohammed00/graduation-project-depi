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
            return await _context.Products.Include(p => p.Merchant).ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByMerchantIdAsync(int merchantId)
        {
            return await _context.Products.Where(p => p.MerchantId == merchantId).ToListAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(m => m.ProductId == id);
        }

        public async Task<Product?> GetProductForDeleteAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Merchant)
                .FirstOrDefaultAsync(m => m.ProductId == id);
        }

        public async Task CreateProductAsync(Product product)
        {
            _context.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateProductAsync(Product product)
        {
            _context.Update(product);
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
            return await _context.Merchants.ToListAsync();
        }
    }
}
