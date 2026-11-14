using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class MerchantService : IMerchantService
    {
        private readonly DmsDbContext _context;

        public MerchantService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Merchant>> GetMerchantsAsync()
        {
            return await _context.Merchants.Include(m => m.Address).AsNoTracking().ToListAsync();
        }

        public async Task<Merchant?> GetMerchantByIdAsync(int id)
        {
            return await _context.Merchants
                .Include(m => m.Address)
                .Include(m => m.Shipments)
                    .ThenInclude(s => s.Address)
                .FirstOrDefaultAsync(m => m.MerchantId == id);
        }

        public async Task<Merchant?> GetMerchantForDeleteAsync(int id)
        {
            return await _context.Merchants
                .Include(m => m.Address)
                .FirstOrDefaultAsync(m => m.MerchantId == id);
        }

        public async Task CreateMerchantAsync(Merchant merchant)
        {
            _context.Add(merchant);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMerchantAsync(Merchant merchant)
        {
            _context.Update(merchant);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMerchantAsync(int id)
        {
            var merchant = await _context.Merchants.FindAsync(id);
            if (merchant != null)
            {
                _context.Merchants.Remove(merchant);
                await _context.SaveChangesAsync();
            }
        }

        public bool MerchantExists(int id)
        {
            return _context.Merchants.Any(e => e.MerchantId == id);
        }

        public async Task<IEnumerable<Address>> GetAddressesAsync()
        {
            return await _context.Addresses.ToListAsync();
        }
    }
}
