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
                .Where(s => s.RouteId == null && s.DriverId == null)
                .ToListAsync();
        }

        public async Task<IEnumerable<Route>> GetActiveRoutesAsync()
        {
            return await _context.Routes
                .Where(r => r.Status == "active")
                .ToListAsync();
        }

        public async Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        {
            var drivers = await _context.Drivers
                .Include(d => d.Zone)
                .Include(d => d.PerformanceScores)
                .Include(d => d.DriverVehicles)
                .Where(d => d.CurrentStatus == "available" && d.DriverVehicles.Any())
                .ToListAsync();

            // Sort by most recent score descending
            return drivers.OrderByDescending(d => 
                d.PerformanceScores.OrderByDescending(ps => ps.RecordedAt).FirstOrDefault()?.DailyScore ?? 0);
        }

        public async Task<(bool IsSuccess, string Message)> AssignShipmentToDriverDirectlyAsync(int shipmentId, int driverId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null) return (false, "Shipment not found.");
            
            var driver = await _context.Drivers.FindAsync(driverId);
            if (driver == null) return (false, "Driver not found.");

            // Directly assign without route
            shipment.DriverId = driverId;
            shipment.RouteId = null; // Ensure it's not in a route

            // Update status
            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                ShipmentId = shipment.ShipmentId,
                Status = "Dispatched",
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (true, "Shipment successfully assigned directly to driver.");
        }

        public async Task<(bool IsSuccess, string Message)> AssignShipmentToRouteAsync(int shipmentId, int routeId)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null) return (false, "Shipment not found.");

            var route = await _context.Routes.FindAsync(routeId);
            if (route == null) return (false, "Route not found.");

            shipment.RouteId = routeId;
            shipment.DriverId = null; // Ensure it's not directly assigned if in a route

            // Update status
            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                ShipmentId = shipment.ShipmentId,
                Status = "Dispatched",
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return (true, "Shipment successfully assigned to route.");
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
                    var result = await AssignShipmentToDriverDirectlyAsync(shipment.ShipmentId, matchingDriver.DriverId);
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
