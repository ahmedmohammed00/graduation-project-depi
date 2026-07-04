using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Services
{
    public class RouteService : IRouteService
    {
        private readonly DmsDbContext _context;

        public RouteService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Route>> GetRoutesAsync()
        {
            return await _context.Routes.ToListAsync();
        }

        public async Task<Route?> GetRouteByIdAsync(int id)
        {
            return await _context.Routes
                .FirstOrDefaultAsync(m => m.RouteId == id);
        }

        public async Task<Route?> GetRouteForDeleteAsync(int id)
        {
            return await _context.Routes
                .FirstOrDefaultAsync(m => m.RouteId == id);
        }

        public async Task CreateRouteAsync(Route route)
        {
            _context.Add(route);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRouteAsync(Route route)
        {
            _context.Update(route);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteRouteAsync(int id)
        {
            var route = await _context.Routes.FindAsync(id);
            if (route != null)
            {
                _context.Routes.Remove(route);
                await _context.SaveChangesAsync();
            }
        }

        public bool RouteExists(int id)
        {
            return _context.Routes.Any(e => e.RouteId == id);
        }
    }
}
