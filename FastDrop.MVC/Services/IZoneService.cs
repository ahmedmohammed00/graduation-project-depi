using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IZoneService
    {
        Task<IEnumerable<Zone>> GetZonesAsync();
        Task<Zone?> GetZoneByIdAsync(int id);
        Task<Zone?> GetZoneForDeleteAsync(int id);
        Task CreateZoneAsync(Zone zone);
        Task UpdateZoneAsync(Zone zone);
        Task DeleteZoneAsync(int id);
        bool ZoneExists(int id);
    }
}
