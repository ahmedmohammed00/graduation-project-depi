using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IDriverService
    {
        Task<IEnumerable<Driver>> GetDriversAsync();
        Task<Driver?> GetDriverByIdAsync(int id);
        Task<Driver?> GetDriverForDeleteAsync(int id);
        Task CreateDriverAsync(Driver driver);
        Task UpdateDriverAsync(Driver driver);
        Task DeleteDriverAsync(int id);
        bool DriverExists(int id);

        Task<IEnumerable<Address>> GetAddressesAsync();
        Task<IEnumerable<Employee>> GetEmployeesAsync();
        Task<IEnumerable<Zone>> GetZonesAsync();
    }
}
