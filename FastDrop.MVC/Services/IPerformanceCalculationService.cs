using System.Threading.Tasks;
using FastDrop.MVC.Models;

namespace FastDrop.MVC.Services
{
    public interface IPerformanceCalculationService
    {
        Task<PerformanceScore> CalculateDriverPerformanceAsync(int driverId);
    }
}
