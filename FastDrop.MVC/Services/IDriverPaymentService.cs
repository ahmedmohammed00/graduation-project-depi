using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IDriverPaymentService
    {
        Task<IEnumerable<DriverPayment>> GetDriverPaymentsAsync();
        Task<DriverPayment?> GetDriverPaymentByIdAsync(int id);
        Task<DriverPayment?> GetDriverPaymentForDeleteAsync(int id);
        Task CreateDriverPaymentAsync(DriverPayment driverPayment);
        Task UpdateDriverPaymentAsync(DriverPayment driverPayment);
        Task DeleteDriverPaymentAsync(int id);
        bool DriverPaymentExists(int id);

        Task<IEnumerable<Driver>> GetDriversAsync();
    }
}
