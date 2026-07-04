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
            return await _context.Addresses.ToListAsync();
        }
    }
}
