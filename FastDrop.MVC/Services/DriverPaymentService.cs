using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Services
{
    public class DriverPaymentService : IDriverPaymentService
    {
        private readonly DmsDbContext _context;

        public DriverPaymentService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DriverPayment>> GetDriverPaymentsAsync()
        {
            return await _context.DriverPayments
                .Include(d => d.Driver)
                .Include(d => d.Shipment)
                    .ThenInclude(s => s.Address)
                .ToListAsync();
        }

        public async Task<DriverPayment?> GetDriverPaymentByIdAsync(int id)
        {
            return await _context.DriverPayments
                .Include(d => d.Driver)
                .Include(d => d.Shipment)
                    .ThenInclude(s => s.Address)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
        }

        public async Task<DriverPayment?> GetDriverPaymentForDeleteAsync(int id)
        {
            return await _context.DriverPayments
                .Include(d => d.Driver)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
        }

        public async Task CreateDriverPaymentAsync(DriverPayment driverPayment)
        {
            _context.Add(driverPayment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDriverPaymentAsync(DriverPayment driverPayment)
        {
            _context.Update(driverPayment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteDriverPaymentAsync(int id)
        {
            var driverPayment = await _context.DriverPayments.FindAsync(id);
            if (driverPayment != null)
            {
                _context.DriverPayments.Remove(driverPayment);
                await _context.SaveChangesAsync();
            }
        }

        public bool DriverPaymentExists(int id)
        {
            return _context.DriverPayments.Any(e => e.PaymentId == id);
        }

        public async Task<IEnumerable<Driver>> GetDriversAsync()
        {
            return await _context.Drivers.ToListAsync();
        }
    }
}
