using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class ZoneService : IZoneService
    {
        private readonly DmsDbContext _context;

        public ZoneService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Zone>> GetZonesAsync()
        {
            return await _context.Zones.ToListAsync();
        }

        public async Task<Zone?> GetZoneByIdAsync(int id)
        {
            return await _context.Zones
                .FirstOrDefaultAsync(m => m.ZoneId == id);
        }

        public async Task<Zone?> GetZoneForDeleteAsync(int id)
        {
            return await _context.Zones
                .FirstOrDefaultAsync(m => m.ZoneId == id);
        }

        public async Task CreateZoneAsync(Zone zone)
        {
            _context.Add(zone);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateZoneAsync(Zone zone)
        {
            _context.Update(zone);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteZoneAsync(int id)
        {
            var zone = await _context.Zones.FindAsync(id);
            if (zone != null)
            {
                _context.Zones.Remove(zone);
                await _context.SaveChangesAsync();
            }
        }

        public bool ZoneExists(int id)
        {
            return _context.Zones.Any(e => e.ZoneId == id);
        }
    }
}
