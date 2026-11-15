using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FastDrop.MVC.Models;
using Microsoft.EntityFrameworkCore;
using Route = FastDrop.MVC.Models.Route;

namespace FastDrop.MVC.Services
{
    public class ShipmentService : IShipmentService
    {
        private readonly DmsDbContext _context;
        private readonly ILogger<ShipmentService>? _logger;

        public ShipmentService(DmsDbContext context, ILogger<ShipmentService>? logger = null)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Shipment>> GetShipmentsAsync(string role, int? merchantId, int? driverId)
        {
            var query = _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Warehouse)
                .AsNoTracking()
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
                
                query = query.Where(s => 
                    (s.RouteId.HasValue && assignedRoutes.Contains(s.RouteId.Value)) || 
                    (s.DriverId == driverId.Value)
                );
            }

            return await query.ToListAsync();
        }

        public async Task<Shipment?> GetShipmentByIdAsync(int id)
        {
            return await ShipmentDetailsQuery().FirstOrDefaultAsync(s => s.ShipmentId == id);
        }

        public async Task<Shipment?> GetVisibleShipmentByIdAsync(int id, ClaimsPrincipal user)
        {
            if (id <= 0 || user.Identity?.IsAuthenticated != true) return null;

            var query = ShipmentDetailsQuery().Where(s => s.ShipmentId == id);
            if (user.IsInRole("SuperAdmin") || user.IsInRole("Dispatcher"))
            {
                return await query.AsNoTracking().FirstOrDefaultAsync();
            }

            if (user.IsInRole("Merchant"))
            {
                var merchantId = GetPositiveClaimId(user, "MerchantId");
                if (!merchantId.HasValue) return null;
                query = query.Where(s => s.MerchantId == merchantId.Value);
            }
            else if (user.IsInRole("Driver"))
            {
                var driverId = GetPositiveClaimId(user, "DriverId");
                if (!driverId.HasValue) return null;
                query = query.Where(s =>
                    (!s.RouteId.HasValue && s.DriverId == driverId.Value) ||
                    (s.RouteId.HasValue && _context.DriverRoutes.Any(dr =>
                        dr.DriverId == driverId.Value && dr.RouteId == s.RouteId.Value)));
            }
            else
            {
                return null;
            }

            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        private static int? GetPositiveClaimId(ClaimsPrincipal user, string claimType)
        {
            var claims = user.FindAll(claimType).ToArray();
            return claims.Length == 1 && int.TryParse(claims[0].Value, out var id) && id > 0 ? id : null;
        }

        private IQueryable<Shipment> ShipmentDetailsQuery() => _context.Shipments
                .Include(s => s.Address)
                .Include(s => s.Merchant)
                .Include(s => s.Route)
                .Include(s => s.Driver)
                .Include(s => s.Warehouse)
                .Include(s => s.ShipmentStatuses)
                .Include(s => s.ShipmentAttempts)
                .Include(s => s.ShipmentProducts)
                    .ThenInclude(sp => sp.Product);

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
                throw new InvalidOperationException("Merchant creation must use the validated merchant order workflow.");
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
            // Preserve the stored owner as the concurrency value, including when staff transfer ownership.
            var stored = await _context.Shipments.SingleAsync(s => s.ShipmentId == shipment.ShipmentId);
            stored.CreationDate = shipment.CreationDate;
            stored.CodAmount = shipment.CodAmount;
            stored.WeightKg = shipment.WeightKg;
            stored.WarehouseId = shipment.WarehouseId;
            stored.MerchantId = shipment.MerchantId;
            stored.RouteId = shipment.RouteId;
            stored.AddressId = shipment.AddressId;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteShipmentAsync(int id)
        {
            var shipment = await _context.Shipments
                .Include(s => s.ShipmentProducts).ThenInclude(p => p.Product)
                .Include(s => s.ShipmentStatuses).Include(s => s.ShipmentAttempts)
                .FirstOrDefaultAsync(s => s.ShipmentId == id);
            if (shipment != null)
            {
                var pending = !shipment.RouteId.HasValue && !shipment.DriverId.HasValue &&
                    shipment.ShipmentAttempts.Count == 0 && shipment.ShipmentStatuses.All(s => s.Status == "Pending");
                if (pending)
                    foreach (var item in shipment.ShipmentProducts.Where(p => p.UnitPrice.HasValue))
                        item.Product.StockQuantity = checked(item.Product.StockQuantity + item.Quantity);
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
                bool isAssigned = false;
                if (shipment.RouteId.HasValue)
                {
                    isAssigned = await _context.DriverRoutes
                        .AnyAsync(dr => dr.DriverId == driverId.Value && dr.RouteId == shipment.RouteId);
                }
                else if (shipment.DriverId.HasValue)
                {
                    isAssigned = shipment.DriverId == driverId.Value;
                }
                
                if (!isAssigned)
                {
                    return (false, true);
                }
            }

            if (shipment.DeliveryConfirmedAt.HasValue || await _context.ShipmentStatuses.AnyAsync(s => s.ShipmentId == id && s.Status == "Delivered"))
                return status == "Delivered" ? (true, false) : (false, true);

            _context.ShipmentStatuses.Add(new ShipmentStatus
            {
                Shipment = shipment,
                Status = status,
                UpdatedAt = DateTime.UtcNow
            });

            if (status == "Delivered")
            {
                shipment.DeliveryConfirmedAt = DateTime.UtcNow;
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
            if (driverId <= 0) return (false, "A valid driver identity is required.");
            var shipment = await _context.Shipments.FindAsync(shipmentId);
            if (shipment == null) return (false, "Shipment not found.");
            if (!await AssignedToDriverAsync(shipment, driverId)) return (false, "You are not assigned to this shipment.");

            var existing = await ExistingDeliveryAsync(shipment, driverId);
            if (existing.HasValue) return existing.Value;

            // Real recipient OTP verification remains a separate S09 integration.
            // This change enforces a single atomic delivery/payout event for the existing workflow.
            if (string.IsNullOrWhiteSpace(otpCode) || otpCode.Length > 100) return (false, "OTP code is required.");
            var fee = await _context.Addresses.Where(a => a.AddressId == shipment.AddressId)
                .Select(a => a.Zone == null ? 0m : a.Zone.DeliveryFee ?? 0m).SingleOrDefaultAsync();
            var payout = decimal.Round(fee > 0 ? fee * 0.5m : 15m, 2, MidpointRounding.AwayFromZero);
            var attemptNumber = (await _context.ShipmentAttempts.Where(a => a.ShipmentId == shipmentId)
                .MaxAsync(a => (int?)a.AttemptNumber) ?? 0) + 1;
            var now = DateTime.UtcNow;
            shipment.DeliveryConfirmedAt = now;
            shipment.ShipmentStatuses.Add(new ShipmentStatus { Status = "Delivered", UpdatedAt = now });
            var attempt = new ShipmentAttempt
            {
                Shipment = shipment, DriverId = driverId, AttemptNumber = attemptNumber,
                Status = "Success", AttemptTime = now
            };
            _context.ShipmentAttempts.Add(attempt);
            _context.PodEvidences.Add(new PodEvidence
            {
                ShipmentAttempt = attempt, PhotoUrl = "/assets/simulated-pod.png",
                OtpVerified = true, Notes = comments
            });
            _context.DriverPayments.Add(new DriverPayment
            {
                DriverId = driverId, ShipmentId = shipmentId, TotalPayout = payout,
                DistanceKm = 0, PaymentStatus = "Pending", CreatedAt = now
            });
            try
            {
                // SQL rowversion plus the unique shipment payout index protect simultaneous retries.
                // The delivery marker, status, attempt, evidence and payout commit together.
                await _context.SaveChangesAsync();
                return (true, "Delivery successful and POD captured.");
            }
            catch (DbUpdateException exception)
            {
                _context.ChangeTracker.Clear();
                var stored = await _context.Shipments.AsNoTracking().SingleOrDefaultAsync(s => s.ShipmentId == shipmentId);
                if (stored != null && await AssignedToDriverAsync(stored, driverId))
                {
                    var completed = await ExistingDeliveryAsync(stored, driverId);
                    if (completed.HasValue && completed.Value.IsSuccess) return completed.Value;
                }
                _logger?.LogWarning(exception, "Delivery confirmation conflicted for shipment {ShipmentId}", shipmentId);
                return (false, "Delivery could not be saved. Reload the shipment and try again.");
            }
        }

        private Task<bool> AssignedToDriverAsync(Shipment shipment, int driverId) => shipment.RouteId.HasValue
            ? _context.DriverRoutes.AnyAsync(dr => dr.RouteId == shipment.RouteId && dr.DriverId == driverId)
            : Task.FromResult(shipment.DriverId == driverId);

        private async Task<(bool IsSuccess, string Message)?> ExistingDeliveryAsync(Shipment shipment, int driverId)
        {
            var payments = await _context.DriverPayments.AsNoTracking().Where(p => p.ShipmentId == shipment.ShipmentId)
                .Select(p => p.DriverId).ToListAsync();
            var delivered = shipment.DeliveryConfirmedAt.HasValue || await _context.ShipmentStatuses
                .AnyAsync(s => s.ShipmentId == shipment.ShipmentId && s.Status == "Delivered");
            if (delivered && payments.Count == 1 && payments[0] == driverId)
                return (true, "Delivery was already recorded. No additional payout was created.");
            if (delivered || payments.Count > 0)
                return (false, "This shipment already has delivery or payment records. Ask an administrator to review them.");
            return null;
        }

        public async Task<IEnumerable<Address>> GetAddressesAsync()
        {
            return await _context.Addresses.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Merchant>> GetMerchantsAsync()
        {
            return await _context.Merchants.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Route>> GetRoutesAsync()
        {
            return await _context.Routes.AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Warehouse>> GetWarehousesAsync()
        {
            return await _context.Warehouses.AsNoTracking().ToListAsync();
        }
    }
}
