using System.Collections.Generic;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Services
{
    public interface IRouteService
    {
        Task<IEnumerable<Route>> GetRoutesAsync();
        Task<Route?> GetRouteByIdAsync(int id);
        Task<Route?> GetRouteForDeleteAsync(int id);
        Task CreateRouteAsync(Route route);
        Task UpdateRouteAsync(Route route);
        Task DeleteRouteAsync(int id);
        bool RouteExists(int id);
        Task<IEnumerable<Route>> GetRoutesByDriverIdAsync(int driverId);
    }
}
