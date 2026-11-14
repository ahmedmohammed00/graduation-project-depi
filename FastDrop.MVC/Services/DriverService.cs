using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class DriverService : IDriverService
    {
        private readonly DmsDbContext _context;

        public DriverService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Driver>> GetDriversAsync()
        {
            return await _context.Drivers
                .Include(d => d.Address)
                .Include(d => d.Employee)
                .Include(d => d.Zone)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Driver?> GetDriverByIdAsync(int id)
        {
            return await _context.Drivers
                .Include(d => d.Address)
                .Include(d => d.Employee)
                .Include(d => d.Zone)
                .FirstOrDefaultAsync(m => m.DriverId == id);
        }

        public async Task<Driver?> GetDriverForDeleteAsync(int id)
        {
            return await _context.Drivers
                .Include(d => d.Address)
                .Include(d => d.Employee)
                .Include(d => d.Zone)
                .FirstOrDefaultAsync(m => m.DriverId == id);
        }

        public async Task CreateDriverAsync(Driver driver)
        {
            _context.Add(driver);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDriverAsync(Driver driver)
        {
            _context.Update(driver);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDriverAsync(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver != null)
            {
                _context.Drivers.Remove(driver);
                await _context.SaveChangesAsync();
            }
        }

        public bool DriverExists(int id)
        {
            return _context.Drivers.Any(e => e.DriverId == id);
        }

        public async Task<IEnumerable<Address>> GetAddressesAsync()
        {
            return await _context.Addresses.ToListAsync();
        }

        public async Task<IEnumerable<Employee>> GetEmployeesAsync()
        {
            return await _context.Employees.ToListAsync();
        }

        public async Task<IEnumerable<Zone>> GetZonesAsync()
        {
            return await _context.Zones.ToListAsync();
        }
        public async Task<IEnumerable<Vehicle>> GetVehiclesAsync()
        {
            return await _context.Vehicles.ToListAsync();
        }

        public async Task AssignVehicleToDriverAsync(int driverId, int vehicleId)
        {
            var existingLink = await _context.DriverVehicles.FirstOrDefaultAsync(dv => dv.DriverId == driverId);
            if (existingLink != null)
            {
                if (existingLink.VehicleId != vehicleId)
                {
                    existingLink.VehicleId = vehicleId;
                    _context.Update(existingLink);
                }
            }
            else
            {
                _context.DriverVehicles.Add(new DriverVehicle { DriverId = driverId, VehicleId = vehicleId });
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int?> GetDriverVehicleIdAsync(int driverId)
        {
            var dv = await _context.DriverVehicles.FirstOrDefaultAsync(dv => dv.DriverId == driverId);
            return dv?.VehicleId;
        }
    }
}
