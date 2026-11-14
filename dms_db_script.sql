IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Role] (
    [RoleId] int NOT NULL IDENTITY,
    [RoleName] nvarchar(max) NOT NULL,
    [PermissionsDescription] nvarchar(max) NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY ([RoleId])
);

CREATE TABLE [Route] (
    [RouteId] int NOT NULL IDENTITY,
    [RouteName] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Route] PRIMARY KEY ([RouteId])
);

CREATE TABLE [Vehicle] (
    [VehicleId] int NOT NULL IDENTITY,
    [LicensePlate] nvarchar(450) NOT NULL,
    [Model] nvarchar(max) NULL,
    [VehicleType] nvarchar(max) NULL,
    [MaxCapacityKg] decimal(8,2) NULL,
    [LicenseExpiryDate] datetime2 NULL,
    CONSTRAINT [PK_Vehicle] PRIMARY KEY ([VehicleId])
);

CREATE TABLE [Zone] (
    [ZoneId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [City] nvarchar(max) NULL,
    [GeojsonBoundary] nvarchar(max) NULL,
    [DeliveryFee] decimal(10,2) NULL,
    [ActiveStatus] bit NOT NULL,
    CONSTRAINT [PK_Zone] PRIMARY KEY ([ZoneId])
);

CREATE TABLE [Employee] (
    [EmployeeId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [AccountStatus] nvarchar(max) NOT NULL,
    [RoleId] int NULL,
    [SupervisorId] int NULL,
    CONSTRAINT [PK_Employee] PRIMARY KEY ([EmployeeId]),
    CONSTRAINT [FK_Employee_Employee_SupervisorId] FOREIGN KEY ([SupervisorId]) REFERENCES [Employee] ([EmployeeId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Employee_Role_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Role] ([RoleId])
);

CREATE TABLE [Address] (
    [AddressId] int NOT NULL IDENTITY,
    [District] nvarchar(max) NULL,
    [Street] nvarchar(max) NULL,
    [Latitude] decimal(10,8) NULL,
    [Longitude] decimal(11,8) NULL,
    [ZoneId] int NULL,
    CONSTRAINT [PK_Address] PRIMARY KEY ([AddressId]),
    CONSTRAINT [FK_Address_Zone_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [Zone] ([ZoneId])
);

CREATE TABLE [Driver] (
    [DriverId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [LicenseNumber] nvarchar(max) NULL,
    [LicenseExpiryDate] datetime2 NULL,
    [CurrentStatus] nvarchar(max) NOT NULL,
    [ZoneId] int NULL,
    [AddressId] int NULL,
    [EmployeeId] int NULL,
    CONSTRAINT [PK_Driver] PRIMARY KEY ([DriverId]),
    CONSTRAINT [FK_Driver_Address_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [Address] ([AddressId]),
    CONSTRAINT [FK_Driver_Employee_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employee] ([EmployeeId]),
    CONSTRAINT [FK_Driver_Zone_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [Zone] ([ZoneId])
);

CREATE TABLE [Merchant] (
    [MerchantId] int NOT NULL IDENTITY,
    [ShopName] nvarchar(max) NOT NULL,
    [Email] nvarchar(450) NOT NULL,
    [Password] nvarchar(max) NOT NULL,
    [ContactName] nvarchar(max) NULL,
    [Phone] nvarchar(max) NULL,
    [Status] nvarchar(max) NOT NULL,
    [AddressId] int NULL,
    CONSTRAINT [PK_Merchant] PRIMARY KEY ([MerchantId]),
    CONSTRAINT [FK_Merchant_Address_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [Address] ([AddressId])
);

CREATE TABLE [Route_Stops] (
    [StopId] int NOT NULL IDENTITY,
    [RouteId] int NOT NULL,
    [AddressId] int NOT NULL,
    [SequenceOrder] int NOT NULL,
    [DeliveredAt] datetime2 NULL,
    CONSTRAINT [PK_Route_Stops] PRIMARY KEY ([StopId]),
    CONSTRAINT [FK_Route_Stops_Address_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [Address] ([AddressId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Route_Stops_Route_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Route] ([RouteId]) ON DELETE CASCADE
);

CREATE TABLE [Warehouse] (
    [WarehouseId] int NOT NULL IDENTITY,
    [WarehouseName] nvarchar(max) NOT NULL,
    [AddressId] int NULL,
    CONSTRAINT [PK_Warehouse] PRIMARY KEY ([WarehouseId]),
    CONSTRAINT [FK_Warehouse_Address_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [Address] ([AddressId])
);

CREATE TABLE [Driver_Payment] (
    [PaymentId] int NOT NULL IDENTITY,
    [DriverId] int NULL,
    [TotalPayout] decimal(10,2) NULL,
    [DistanceKm] decimal(10,2) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Driver_Payment] PRIMARY KEY ([PaymentId]),
    CONSTRAINT [FK_Driver_Payment_Driver_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Driver] ([DriverId])
);

CREATE TABLE [Driver_Route] (
    [DriverId] int NOT NULL,
    [RouteId] int NOT NULL,
    [RoutePay] decimal(10,2) NULL,
    CONSTRAINT [PK_Driver_Route] PRIMARY KEY ([DriverId], [RouteId]),
    CONSTRAINT [FK_Driver_Route_Driver_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Driver] ([DriverId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Driver_Route_Route_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Route] ([RouteId]) ON DELETE CASCADE
);

CREATE TABLE [Driver_Vehicle] (
    [DriverId] int NOT NULL,
    [VehicleId] int NOT NULL,
    CONSTRAINT [PK_Driver_Vehicle] PRIMARY KEY ([DriverId], [VehicleId]),
    CONSTRAINT [FK_Driver_Vehicle_Driver_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Driver] ([DriverId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Driver_Vehicle_Vehicle_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicle] ([VehicleId]) ON DELETE CASCADE
);

CREATE TABLE [Performance_Score] (
    [PerfId] int NOT NULL IDENTITY,
    [DriverId] int NULL,
    [DailyScore] decimal(5,2) NULL,
    [Tier] nvarchar(max) NULL,
    [WeightedHistory] decimal(5,2) NULL,
    [RecordedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Performance_Score] PRIMARY KEY ([PerfId]),
    CONSTRAINT [FK_Performance_Score_Driver_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Driver] ([DriverId])
);

CREATE TABLE [Product] (
    [ProductId] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NULL,
    [Price] decimal(10,2) NOT NULL,
    [StockQuantity] int NOT NULL,
    [MerchantId] int NULL,
    CONSTRAINT [PK_Product] PRIMARY KEY ([ProductId]),
    CONSTRAINT [FK_Product_Merchant_MerchantId] FOREIGN KEY ([MerchantId]) REFERENCES [Merchant] ([MerchantId])
);

CREATE TABLE [Shipment] (
    [ShipmentId] int NOT NULL IDENTITY,
    [CreationDate] datetime2 NOT NULL,
    [CodAmount] decimal(10,2) NOT NULL,
    [WeightKg] decimal(8,2) NULL,
    [WarehouseId] int NULL,
    [MerchantId] int NULL,
    [RouteId] int NULL,
    [AddressId] int NULL,
    CONSTRAINT [PK_Shipment] PRIMARY KEY ([ShipmentId]),
    CONSTRAINT [FK_Shipment_Address_AddressId] FOREIGN KEY ([AddressId]) REFERENCES [Address] ([AddressId]),
    CONSTRAINT [FK_Shipment_Merchant_MerchantId] FOREIGN KEY ([MerchantId]) REFERENCES [Merchant] ([MerchantId]),
    CONSTRAINT [FK_Shipment_Route_RouteId] FOREIGN KEY ([RouteId]) REFERENCES [Route] ([RouteId]),
    CONSTRAINT [FK_Shipment_Warehouse_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouse] ([WarehouseId])
);

CREATE TABLE [Product_Warehouse] (
    [ProductId] int NOT NULL,
    [WarehouseId] int NOT NULL,
    CONSTRAINT [PK_Product_Warehouse] PRIMARY KEY ([ProductId], [WarehouseId]),
    CONSTRAINT [FK_Product_Warehouse_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Product_Warehouse_Warehouse_WarehouseId] FOREIGN KEY ([WarehouseId]) REFERENCES [Warehouse] ([WarehouseId]) ON DELETE CASCADE
);

CREATE TABLE [Feedback] (
    [FeedbackId] int NOT NULL IDENTITY,
    [ShipmentId] int NULL,
    [DriverId] int NULL,
    [Rating] decimal(3,1) NULL,
    [Comment] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Feedback] PRIMARY KEY ([FeedbackId]),
    CONSTRAINT [FK_Feedback_Driver_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Driver] ([DriverId]),
    CONSTRAINT [FK_Feedback_Shipment_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipment] ([ShipmentId])
);

CREATE TABLE [Invoice] (
    [InvoiceId] int NOT NULL IDENTITY,
    [ShipmentId] int NULL,
    [MerchantId] int NULL,
    [AmountDue] decimal(10,2) NOT NULL,
    [PaymentStatus] nvarchar(max) NOT NULL,
    [PeriodStart] datetime2 NULL,
    [PeriodEnd] datetime2 NULL,
    CONSTRAINT [PK_Invoice] PRIMARY KEY ([InvoiceId]),
    CONSTRAINT [FK_Invoice_Merchant_MerchantId] FOREIGN KEY ([MerchantId]) REFERENCES [Merchant] ([MerchantId]),
    CONSTRAINT [FK_Invoice_Shipment_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipment] ([ShipmentId])
);

CREATE TABLE [Shipment_Attempt] (
    [AttemptId] int NOT NULL IDENTITY,
    [ShipmentId] int NOT NULL,
    [DriverId] int NULL,
    [AttemptNumber] int NOT NULL,
    [Status] nvarchar(max) NULL,
    [OtpCode] nvarchar(max) NULL,
    [PhotoUrl] nvarchar(max) NULL,
    [Reason] nvarchar(max) NULL,
    [AttemptTime] datetime2 NOT NULL,
    CONSTRAINT [PK_Shipment_Attempt] PRIMARY KEY ([AttemptId]),
    CONSTRAINT [FK_Shipment_Attempt_Driver_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Driver] ([DriverId]),
    CONSTRAINT [FK_Shipment_Attempt_Shipment_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipment] ([ShipmentId]) ON DELETE CASCADE
);

CREATE TABLE [Shipment_Product] (
    [ShipmentId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    CONSTRAINT [PK_Shipment_Product] PRIMARY KEY ([ShipmentId], [ProductId]),
    CONSTRAINT [FK_Shipment_Product_Product_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Product] ([ProductId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Shipment_Product_Shipment_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipment] ([ShipmentId]) ON DELETE CASCADE
);

CREATE TABLE [Shipment_Status] (
    [StatusId] int NOT NULL IDENTITY,
    [ShipmentId] int NOT NULL,
    [Status] nvarchar(max) NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Shipment_Status] PRIMARY KEY ([StatusId]),
    CONSTRAINT [FK_Shipment_Status_Shipment_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipment] ([ShipmentId]) ON DELETE CASCADE
);

CREATE TABLE [POD_Evidence] (
    [AttemptId] int NOT NULL,
    [PhotoUrl] nvarchar(max) NULL,
    [OtpVerified] bit NOT NULL,
    [Notes] nvarchar(max) NULL,
    CONSTRAINT [PK_POD_Evidence] PRIMARY KEY ([AttemptId]),
    CONSTRAINT [FK_POD_Evidence_Shipment_Attempt_AttemptId] FOREIGN KEY ([AttemptId]) REFERENCES [Shipment_Attempt] ([AttemptId]) ON DELETE CASCADE
);

CREATE INDEX [IX_Address_ZoneId] ON [Address] ([ZoneId]);

CREATE INDEX [IX_Driver_AddressId] ON [Driver] ([AddressId]);

CREATE INDEX [IX_Driver_EmployeeId] ON [Driver] ([EmployeeId]);

CREATE INDEX [IX_Driver_ZoneId] ON [Driver] ([ZoneId]);

CREATE INDEX [IX_Driver_Payment_DriverId] ON [Driver_Payment] ([DriverId]);

CREATE INDEX [IX_Driver_Route_RouteId] ON [Driver_Route] ([RouteId]);

CREATE INDEX [IX_Driver_Vehicle_VehicleId] ON [Driver_Vehicle] ([VehicleId]);

CREATE UNIQUE INDEX [IX_Employee_Email] ON [Employee] ([Email]);

CREATE INDEX [IX_Employee_RoleId] ON [Employee] ([RoleId]);

CREATE INDEX [IX_Employee_SupervisorId] ON [Employee] ([SupervisorId]);

CREATE INDEX [IX_Feedback_DriverId] ON [Feedback] ([DriverId]);

CREATE INDEX [IX_Feedback_ShipmentId] ON [Feedback] ([ShipmentId]);

CREATE INDEX [IX_Invoice_MerchantId] ON [Invoice] ([MerchantId]);

CREATE INDEX [IX_Invoice_ShipmentId] ON [Invoice] ([ShipmentId]);

CREATE INDEX [IX_Merchant_AddressId] ON [Merchant] ([AddressId]);

CREATE UNIQUE INDEX [IX_Merchant_Email] ON [Merchant] ([Email]);

CREATE INDEX [IX_Performance_Score_DriverId] ON [Performance_Score] ([DriverId]);

CREATE INDEX [IX_Product_MerchantId] ON [Product] ([MerchantId]);

CREATE INDEX [IX_Product_Warehouse_WarehouseId] ON [Product_Warehouse] ([WarehouseId]);

CREATE INDEX [IX_Route_Stops_AddressId] ON [Route_Stops] ([AddressId]);

CREATE INDEX [IX_Route_Stops_RouteId] ON [Route_Stops] ([RouteId]);

CREATE INDEX [IX_Shipment_AddressId] ON [Shipment] ([AddressId]);

CREATE INDEX [IX_Shipment_MerchantId] ON [Shipment] ([MerchantId]);

CREATE INDEX [IX_Shipment_RouteId] ON [Shipment] ([RouteId]);

CREATE INDEX [IX_Shipment_WarehouseId] ON [Shipment] ([WarehouseId]);

CREATE INDEX [IX_Shipment_Attempt_DriverId] ON [Shipment_Attempt] ([DriverId]);

CREATE INDEX [IX_Shipment_Attempt_ShipmentId] ON [Shipment_Attempt] ([ShipmentId]);

CREATE INDEX [IX_Shipment_Product_ProductId] ON [Shipment_Product] ([ProductId]);

CREATE INDEX [IX_Shipment_Status_ShipmentId] ON [Shipment_Status] ([ShipmentId]);

CREATE UNIQUE INDEX [IX_Vehicle_LicensePlate] ON [Vehicle] ([LicensePlate]);

CREATE INDEX [IX_Warehouse_AddressId] ON [Warehouse] ([AddressId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260703231759_InitialCreate', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260704060255_InitialMigration', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Driver_Payment] ADD [PaymentStatus] nvarchar(max) NOT NULL DEFAULT N'';

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260707142822_AddPaymentStatusToDriverPayment', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Zone] ADD [CodAmount] decimal(18,2) NOT NULL DEFAULT 0.0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260709010759_AddZoneCodAmount', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Driver_Payment] ADD [ShipmentId] int NULL;

CREATE INDEX [IX_Driver_Payment_ShipmentId] ON [Driver_Payment] ([ShipmentId]);

ALTER TABLE [Driver_Payment] ADD CONSTRAINT [FK_Driver_Payment_Shipment_ShipmentId] FOREIGN KEY ([ShipmentId]) REFERENCES [Shipment] ([ShipmentId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260709135154_AddShipmentToDriverPayment', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'PermissionsDescription', N'RoleName') AND [object_id] = OBJECT_ID(N'[Role]'))
    SET IDENTITY_INSERT [Role] ON;
INSERT INTO [Role] ([RoleId], [PermissionsDescription], [RoleName])
VALUES (1, N'Full access', N'SuperAdmin'),
(2, N'Manage shipments and drivers', N'Dispatcher'),
(3, N'General employee', N'Employee');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'PermissionsDescription', N'RoleName') AND [object_id] = OBJECT_ID(N'[Role]'))
    SET IDENTITY_INSERT [Role] OFF;

UPDATE Employee SET RoleId = 1 WHERE Email IN ('admin@test.com', 'ahmedmohammed227a@gmail.com')

UPDATE Employee SET RoleId = 2 WHERE Email IN ('dispatcher@test.com', 'dispbatecher@test.com')

UPDATE Employee SET RoleId = 3 WHERE RoleId IS NULL

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260709142926_SeedRoles', N'10.0.9');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Product_Warehouse] ADD [Quantity] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260709164700_AddQuantityToProductWarehouse', N'10.0.9');

COMMIT;
GO

