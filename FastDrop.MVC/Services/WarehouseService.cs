using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class WarehouseService : IWarehouseService
    {
        private readonly DmsDbContext _context;

        public WarehouseService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Warehouse>> GetWarehousesAsync()
        {
            return await _context.Warehouses.Include(w => w.Address).ToListAsync();
        }

        public async Task<Warehouse?> GetWarehouseByIdAsync(int id)
        {
            return await _context.Warehouses
                .Include(w => w.Address)
                .FirstOrDefaultAsync(m => m.WarehouseId == id);
        }

        public async Task<Warehouse?> GetWarehouseForDeleteAsync(int id)
        {
            return await _context.Warehouses
                .Include(w => w.Address)
                .FirstOrDefaultAsync(m => m.WarehouseId == id);
        }

        public async Task CreateWarehouseAsync(Warehouse warehouse)
        {
            _context.Add(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateWarehouseAsync(Warehouse warehouse)
        {
            _context.Update(warehouse);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteWarehouseAsync(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse != null)
            {
                _context.Warehouses.Remove(warehouse);
                await _context.SaveChangesAsync();
            }
        }

        public bool WarehouseExists(int id)
        {
            return _context.Warehouses.Any(e => e.WarehouseId == id);
        }

        public async Task<IEnumerable<Address>> GetAddressesAsync()
        {
            return await _context.Addresses.Include(a => a.Zone).ToListAsync();
        }

        public async Task<Address?> GetAddressByIdAsync(int id)
        {
            return await _context.Addresses.Include(a => a.Zone).FirstOrDefaultAsync(a => a.AddressId == id);
        }

        public async Task CreateAddressAsync(Address address)
        {
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAddressAsync(Address address)
        {
            _context.Addresses.Update(address);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAddressAsync(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address != null)
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<ProductWarehouse>> GetProductsForWarehouseAsync(int warehouseId)
        {
            return await _context.ProductWarehouses
                .Include(pw => pw.Product)
                .ThenInclude(p => p.Merchant)
                .Where(pw => pw.WarehouseId == warehouseId)
                .ToListAsync();
        }

        public async Task AssignProductsToWarehouseAsync(int warehouseId, IEnumerable<int> productIds)
        {
            // Remove existing associations not in the new list (or just remove all and add new)
            var existing = await _context.ProductWarehouses.Where(pw => pw.WarehouseId == warehouseId).ToListAsync();
            _context.ProductWarehouses.RemoveRange(existing);

            // Add new associations
            if (productIds != null && productIds.Any())
            {
                foreach (var pid in productIds)
                {
                    _context.ProductWarehouses.Add(new ProductWarehouse
                    {
                        WarehouseId = warehouseId,
                        ProductId = pid
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
