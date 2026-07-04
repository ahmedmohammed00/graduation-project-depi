using Microsoft.EntityFrameworkCore;

namespace FastDrop.MVC.Models
{
    public class DmsDbContext : DbContext
    {
        public DmsDbContext(DbContextOptions<DmsDbContext> options) : base(options) { }

        public DbSet<Role> Roles { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Merchant> Merchants { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductWarehouse> ProductWarehouses { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<DriverVehicle> DriverVehicles { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStops> RouteStops { get; set; }
        public DbSet<DriverRoute> DriverRoutes { get; set; }
        public DbSet<Shipment> Shipments { get; set; }
        public DbSet<ShipmentStatus> ShipmentStatuses { get; set; }
        public DbSet<ShipmentProduct> ShipmentProducts { get; set; }
        public DbSet<ShipmentAttempt> ShipmentAttempts { get; set; }
        public DbSet<PodEvidence> PodEvidences { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<DriverPayment> DriverPayments { get; set; }
        public DbSet<PerformanceScore> PerformanceScores { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Table names (match SQL exactly)
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<Employee>().ToTable("Employee");
            modelBuilder.Entity<Zone>().ToTable("Zone");
            modelBuilder.Entity<Address>().ToTable("Address");
            modelBuilder.Entity<Merchant>().ToTable("Merchant");
            modelBuilder.Entity<Warehouse>().ToTable("Warehouse");
            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<ProductWarehouse>().ToTable("Product_Warehouse");
            modelBuilder.Entity<Driver>().ToTable("Driver");
            modelBuilder.Entity<Vehicle>().ToTable("Vehicle");
            modelBuilder.Entity<DriverVehicle>().ToTable("Driver_Vehicle");
            modelBuilder.Entity<Route>().ToTable("Route");
            modelBuilder.Entity<RouteStops>().ToTable("Route_Stops");
            modelBuilder.Entity<DriverRoute>().ToTable("Driver_Route");
            modelBuilder.Entity<Shipment>().ToTable("Shipment");
            modelBuilder.Entity<ShipmentStatus>().ToTable("Shipment_Status");
            modelBuilder.Entity<ShipmentProduct>().ToTable("Shipment_Product");
            modelBuilder.Entity<ShipmentAttempt>().ToTable("Shipment_Attempt");
            modelBuilder.Entity<PodEvidence>().ToTable("POD_Evidence");
            modelBuilder.Entity<Invoice>().ToTable("Invoice");
            modelBuilder.Entity<Feedback>().ToTable("Feedback");
            modelBuilder.Entity<DriverPayment>().ToTable("Driver_Payment");
            modelBuilder.Entity<PerformanceScore>().ToTable("Performance_Score");

            // Keys
            modelBuilder.Entity<ProductWarehouse>().HasKey(pw => new { pw.ProductId, pw.WarehouseId });
            modelBuilder.Entity<DriverVehicle>().HasKey(dv => new { dv.DriverId, dv.VehicleId });
            modelBuilder.Entity<DriverRoute>().HasKey(dr => new { dr.DriverId, dr.RouteId });
            modelBuilder.Entity<ShipmentProduct>().HasKey(sp => new { sp.ShipmentId, sp.ProductId });
            modelBuilder.Entity<RouteStops>().HasKey(rs => rs.StopId);
            modelBuilder.Entity<ShipmentStatus>().HasKey(ss => ss.StatusId);
            modelBuilder.Entity<ShipmentAttempt>().HasKey(sa => sa.AttemptId);
            modelBuilder.Entity<DriverPayment>().HasKey(dp => dp.PaymentId);
            modelBuilder.Entity<PerformanceScore>().HasKey(ps => ps.PerfId);
            
            // 1-to-1 for POD_Evidence
            modelBuilder.Entity<PodEvidence>()
                .HasKey(pe => pe.AttemptId);
            modelBuilder.Entity<PodEvidence>()
                .HasOne(pe => pe.ShipmentAttempt)
                .WithOne(sa => sa.PodEvidence)
                .HasForeignKey<PodEvidence>(pe => pe.AttemptId);

            // Employee Supervisor (self-referencing)
            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Supervisor)
                .WithMany(e => e.Subordinates)
                .HasForeignKey(e => e.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precision for decimals
            modelBuilder.Entity<Zone>().Property(z => z.DeliveryFee).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Address>().Property(a => a.Latitude).HasColumnType("decimal(10,8)");
            modelBuilder.Entity<Address>().Property(a => a.Longitude).HasColumnType("decimal(11,8)");
            modelBuilder.Entity<Product>().Property(p => p.Price).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Vehicle>().Property(v => v.MaxCapacityKg).HasColumnType("decimal(8,2)");
            modelBuilder.Entity<DriverRoute>().Property(dr => dr.RoutePay).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Shipment>().Property(s => s.CodAmount).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Shipment>().Property(s => s.WeightKg).HasColumnType("decimal(8,2)");
            modelBuilder.Entity<Invoice>().Property(i => i.AmountDue).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<Feedback>().Property(f => f.Rating).HasColumnType("decimal(3,1)");
            modelBuilder.Entity<DriverPayment>().Property(dp => dp.TotalPayout).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<DriverPayment>().Property(dp => dp.DistanceKm).HasColumnType("decimal(10,2)");
            modelBuilder.Entity<PerformanceScore>().Property(ps => ps.DailyScore).HasColumnType("decimal(5,2)");
            modelBuilder.Entity<PerformanceScore>().Property(ps => ps.WeightedHistory).HasColumnType("decimal(5,2)");
            
            // Uniques
            modelBuilder.Entity<Employee>().HasIndex(e => e.Email).IsUnique();
            modelBuilder.Entity<Merchant>().HasIndex(m => m.Email).IsUnique();
            modelBuilder.Entity<Vehicle>().HasIndex(v => v.LicensePlate).IsUnique();
            
            // Avoid Cascade cycles
            modelBuilder.Entity<ShipmentAttempt>()
                .HasOne(sa => sa.Driver)
                .WithMany(d => d.ShipmentAttempts)
                .HasForeignKey(sa => sa.DriverId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Driver)
                .WithMany(d => d.Feedbacks)
                .HasForeignKey(f => f.DriverId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
