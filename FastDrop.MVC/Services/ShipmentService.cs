using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly DmsDbContext _context;

        public ShipmentService(DmsDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Shipment>> GetShipmentsAsync(string role, int? merchantId, int? driverId)
        {
            var query = _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .AsQueryable();

            if (role == "Merchant" && merchantId.HasValue)
            {
                query = query.Where(s => s.MerchantId == merchantId.Value);
            }
            else if (role == "Driver" && driverId.HasValue)
            {
                var assignedRoutes = await _context.DriverRoutes
                    .Where(dr => dr.DriverId == driverId.Value)
                    .Select(dr => dr.RouteId)
                    .ToListAsync();
                
                query = query.Where(s => s.RouteId.HasValue && assignedRoutes.Contains(s.RouteId.Value));
            }

            return await query.ToListAsync();
        }

        public async Task<Shipment?> GetShipmentByIdAsync(int id)
        {
            return await _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .Include(s => s.ShipmentStatuses)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);
        }

        public async Task<Shipment?> GetShipmentForDeleteAsync(int id)
        {
            return await _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(m => m.ShipmentId == id);
        }

        public async Task CreateShipmentAsync(Shipment shipment, int? merchantId)
        {
            if (merchantId.HasValue)
            {
                shipment.MerchantId = merchantId.Value;
            }

            _context.Add(shipment);
            
            // Add initial pending status
            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                Shipment = shipment,
                Status = "Pending",
                UpdatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }

        public async Task UpdateShipmentAsync(Shipment shipment)
        {
            _context.Update(shipment);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteShipmentAsync(int id)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment != null)
            {
                _context.Shipments.Remove(shipment);
                await _context.SaveChangesAsync();
            }
        }

        public bool ShipmentExists(int id)
        {
            return _context.Shipments.Any(e => e.ShipmentId == id);
        }

        public async Task<(bool IsSuccess, bool IsForbidden)> UpdateStatusAsync(int id, string status, string? comments, string role, int? driverId)
        {
            var shipment = await _context.Shipments.FindAsync(id);
            if (shipment == null)
            {
                return (false, false);
            }

            if (role == "Driver" && driverId.HasValue)
            {
                var isAssigned = await _context.DriverRoutes
                    .AnyAsync(dr => dr.DriverId == driverId.Value && dr.RouteId == shipment.RouteId);
                
                if (!isAssigned)
                {
                    return (false, true);
                }
            }

            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                Shipment = shipment,
                Status = status,
                UpdatedAt = DateTime.UtcNow
            });

            if (status == "Delivered")
            {
                var attempt = new ShipmentAttempt
                {
                    Shipment = shipment,
                    AttemptNumber = 1,
                    Status = "Success",
                    AttemptTime = DateTime.UtcNow
                };

                _context.ShipmentAttempts.Add(attempt);

                _context.PodEvidences.Add(new PodEvidence
                {
                    ShipmentAttempt = attempt,
                    PhotoUrl = "signature.png", // Mocked for now
                    OtpVerified = true,
                    Notes = comments
                });
            }

            await _context.SaveChangesAsync();
            return (true, false);
        }

        public async Task<(bool IsSuccess, string Message)> SubmitPodAsync(int shipmentId, int driverId, string otpCode, string? comments)
        {
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null)
            {
                return (false, "Shipment not found.");
            }

            var isAssigned = await _context.DriverRoutes
                .AnyAsync(dr => dr.DriverId == driverId && dr.RouteId == shipment.RouteId);
            
            if (!isAssigned)
            {
                return (false, "You are not assigned to this shipment's route.");
            }

            // Simulate OTP verification (any OTP is considered valid for simulation)
            if (string.IsNullOrWhiteSpace(otpCode))
            {
                return (false, "OTP code is required.");
            }

            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                Shipment = shipment,
                Status = "Delivered",
                UpdatedAt = DateTime.UtcNow
            });

            var attempt = new ShipmentAttempt
            {
                Shipment = shipment,
                DriverId = driverId,
                AttemptNumber = 1,
                Status = "Success",
                OtpCode = otpCode,
                AttemptTime = DateTime.UtcNow
            };

            _context.ShipmentAttempts.Add(attempt);

            _context.PodEvidences.Add(new PodEvidence
            {
                ShipmentAttempt = attempt,
                PhotoUrl = "/assets/simulated-pod.png", // Simulated POD
                OtpVerified = true,
                Notes = comments
            });

            await _context.SaveChangesAsync();
            return (true, "Delivery successful and POD captured.");
        }

        public async Task<IEnumerable<Address>> GetAddressesAsync()
        {
            return await _context.Addresses.ToListAsync();
        }

        public async Task<IEnumerable<Merchant>> GetMerchantsAsync()
        {
            return await _context.Merchants.ToListAsync();
        }

        public async Task<IEnumerable<Route>> GetRoutesAsync()
        {
            return await _context.Routes.ToListAsync();
        }

        public async Task<IEnumerable<Warehouse>> GetWarehousesAsync()
        {
            return await _context.Warehouses.ToListAsync();
        }
    }
}
