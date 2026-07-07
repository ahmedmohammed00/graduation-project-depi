using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Services
{
    public class DispatcherService : IDispatcherService
    {
        private readonly DmsDbContext _context;

        public DispatcherService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Shipment>> GetUnassignedShipmentsAsync()
        {
            return await _context.Shipments
                .Include(s => s.Address)
                    .ThenInclude(a => a.Zone)
                .Include(s => s.Merchant)
                .Where(s => s.RouteId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        {
            var drivers = await _context.Drivers
                .Include(d => d.Zone)
                .Include(d => d.PerformanceScores)
                .Where(d => d.CurrentStatus == "available")
                .ToListAsync();

            // Sort by most recent score descending
            return drivers.OrderByDescending(d => 
                d.PerformanceScores.OrderByDescending(ps => ps.RecordedAt).FirstOrDefault()?.DailyScore ?? 0);
        }

        public async Task<(bool IsSuccess, string Message)> AssignShipmentToDriverAsync(int shipmentId, int driverId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null) return (false, "Shipment not found.");
            
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null) return (false, "Driver not found.");

            // Find an active route for this driver
            var activeRouteId = await _context.DriverRoutes
                .Where(dr => dr.DriverId == driverId && dr.Route.Status == "active")
                .Select(dr => (int?)dr.RouteId)
                .FirstOrDefaultAsync();

            if (!activeRouteId.HasValue)
            {
                // Create new route
                var newRoute = new Route
                {
                    RouteName = $"Route for {driver.Name} - {DateTime.UtcNow:MMM dd}",
                    Status = "active"
                };
                _context.Routes.Add(newRoute);
                await _context.SaveChangesAsync(); // save to get RouteId

                _context.DriverRoutes.Add(new DriverRoute
                {
                    DriverId = driverId,
                    RouteId = newRoute.RouteId,
                    RoutePay = 0
                });
                
                activeRouteId = newRoute.RouteId;
            }

            shipment.RouteId = activeRouteId.Value;

            // Update status
            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                ShipmentId = shipment.ShipmentId,
                Status = "Dispatched",
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (true, "Shipment successfully assigned.");
        }

        public async Task<int> SmartAssignShipmentsAsync()
        {
            var unassignedShipments = await GetUnassignedShipmentsAsync();
            var availableDrivers = await GetAvailableDriversAsync();

            int assignedCount = 0;

            foreach (var shipment in unassignedShipments)
            {
                if (shipment.Address?.ZoneId == null) continue;

                // Find a driver whose ZoneId matches the Shipment's ZoneId
                var matchingDriver = availableDrivers
                    .FirstOrDefault(d => d.ZoneId == shipment.Address.ZoneId);

                if (matchingDriver != null)
                {
                    var result = await AssignShipmentToDriverAsync(shipment.ShipmentId, matchingDriver.DriverId);
                    if (result.IsSuccess)
                    {
                        assignedCount++;
                    }
                }
            }

            return assignedCount;
        }
    }
}
