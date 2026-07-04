-- =============-- ============================================================
-- DMS (Delivery Management System) - SQL Table Definitions
-- Microsoft SQL Server (T-SQL) Compatible
-- ============================================================

-- ============================================================
-- CREATE DATABASE
-- ============================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'dms_db')
BEGIN
    CREATE DATABASE dms_db;
END
GO

USE dms_db;
GO

-- ============================================================
-- STEP 1: DROP ALL FOREIGN KEY CONSTRAINTS FIRST
-- Must be done before dropping any table
-- ============================================================

-- Performance_Score
IF OBJECT_ID('dbo.Performance_Score',   'U') IS NOT NULL ALTER TABLE dbo.Performance_Score   DROP CONSTRAINT IF EXISTS FK_PS_Driver;
-- Driver_Payment
IF OBJECT_ID('dbo.Driver_Payment',      'U') IS NOT NULL ALTER TABLE dbo.Driver_Payment      DROP CONSTRAINT IF EXISTS FK_DP_Driver;
-- Feedback
IF OBJECT_ID('dbo.Feedback',            'U') IS NOT NULL ALTER TABLE dbo.Feedback            DROP CONSTRAINT IF EXISTS FK_Feedback_Shipment;
IF OBJECT_ID('dbo.Feedback',            'U') IS NOT NULL ALTER TABLE dbo.Feedback            DROP CONSTRAINT IF EXISTS FK_Feedback_Driver;
-- Invoice
IF OBJECT_ID('dbo.Invoice',             'U') IS NOT NULL ALTER TABLE dbo.Invoice             DROP CONSTRAINT IF EXISTS FK_Invoice_Shipment;
IF OBJECT_ID('dbo.Invoice',             'U') IS NOT NULL ALTER TABLE dbo.Invoice             DROP CONSTRAINT IF EXISTS FK_Invoice_Merchant;
-- POD_Evidence
IF OBJECT_ID('dbo.POD_Evidence',        'U') IS NOT NULL ALTER TABLE dbo.POD_Evidence        DROP CONSTRAINT IF EXISTS FK_POD_Attempt;
-- Shipment_Attempt
IF OBJECT_ID('dbo.Shipment_Attempt',    'U') IS NOT NULL ALTER TABLE dbo.Shipment_Attempt    DROP CONSTRAINT IF EXISTS FK_Attempt_Shipment;
IF OBJECT_ID('dbo.Shipment_Attempt',    'U') IS NOT NULL ALTER TABLE dbo.Shipment_Attempt    DROP CONSTRAINT IF EXISTS FK_Attempt_Driver;
-- Shipment_Product
IF OBJECT_ID('dbo.Shipment_Product',    'U') IS NOT NULL ALTER TABLE dbo.Shipment_Product    DROP CONSTRAINT IF EXISTS FK_SP_Shipment;
IF OBJECT_ID('dbo.Shipment_Product',    'U') IS NOT NULL ALTER TABLE dbo.Shipment_Product    DROP CONSTRAINT IF EXISTS FK_SP_Product;
-- Shipment_Status
IF OBJECT_ID('dbo.Shipment_Status',     'U') IS NOT NULL ALTER TABLE dbo.Shipment_Status     DROP CONSTRAINT IF EXISTS FK_SS_Shipment;
-- Shipment
IF OBJECT_ID('dbo.Shipment',            'U') IS NOT NULL ALTER TABLE dbo.Shipment            DROP CONSTRAINT IF EXISTS FK_Shipment_Warehouse;
IF OBJECT_ID('dbo.Shipment',            'U') IS NOT NULL ALTER TABLE dbo.Shipment            DROP CONSTRAINT IF EXISTS FK_Shipment_Merchant;
IF OBJECT_ID('dbo.Shipment',            'U') IS NOT NULL ALTER TABLE dbo.Shipment            DROP CONSTRAINT IF EXISTS FK_Shipment_Route;
IF OBJECT_ID('dbo.Shipment',            'U') IS NOT NULL ALTER TABLE dbo.Shipment            DROP CONSTRAINT IF EXISTS FK_Shipment_Address;
-- Driver_Route
IF OBJECT_ID('dbo.Driver_Route',        'U') IS NOT NULL ALTER TABLE dbo.Driver_Route        DROP CONSTRAINT IF EXISTS FK_DR_Driver;
IF OBJECT_ID('dbo.Driver_Route',        'U') IS NOT NULL ALTER TABLE dbo.Driver_Route        DROP CONSTRAINT IF EXISTS FK_DR_Route;
-- Route_Stops
IF OBJECT_ID('dbo.Route_Stops',         'U') IS NOT NULL ALTER TABLE dbo.Route_Stops         DROP CONSTRAINT IF EXISTS FK_RS_Route;
IF OBJECT_ID('dbo.Route_Stops',         'U') IS NOT NULL ALTER TABLE dbo.Route_Stops         DROP CONSTRAINT IF EXISTS FK_RS_Address;
-- Driver_Vehicle
IF OBJECT_ID('dbo.Driver_Vehicle',      'U') IS NOT NULL ALTER TABLE dbo.Driver_Vehicle      DROP CONSTRAINT IF EXISTS FK_DV_Driver;
IF OBJECT_ID('dbo.Driver_Vehicle',      'U') IS NOT NULL ALTER TABLE dbo.Driver_Vehicle      DROP CONSTRAINT IF EXISTS FK_DV_Vehicle;
-- Product_Warehouse
IF OBJECT_ID('dbo.Product_Warehouse',   'U') IS NOT NULL ALTER TABLE dbo.Product_Warehouse   DROP CONSTRAINT IF EXISTS FK_PW_Product;
IF OBJECT_ID('dbo.Product_Warehouse',   'U') IS NOT NULL ALTER TABLE dbo.Product_Warehouse   DROP CONSTRAINT IF EXISTS FK_PW_Warehouse;
-- Product
IF OBJECT_ID('dbo.Product',             'U') IS NOT NULL ALTER TABLE dbo.Product             DROP CONSTRAINT IF EXISTS FK_Product_Merchant;
-- Driver
IF OBJECT_ID('dbo.Driver',              'U') IS NOT NULL ALTER TABLE dbo.Driver              DROP CONSTRAINT IF EXISTS FK_Driver_Zone;
IF OBJECT_ID('dbo.Driver',              'U') IS NOT NULL ALTER TABLE dbo.Driver              DROP CONSTRAINT IF EXISTS FK_Driver_Address;
IF OBJECT_ID('dbo.Driver',              'U') IS NOT NULL ALTER TABLE dbo.Driver              DROP CONSTRAINT IF EXISTS FK_Driver_Employee;
-- Warehouse
IF OBJECT_ID('dbo.Warehouse',           'U') IS NOT NULL ALTER TABLE dbo.Warehouse           DROP CONSTRAINT IF EXISTS FK_Warehouse_Address;
-- Merchant
IF OBJECT_ID('dbo.Merchant',            'U') IS NOT NULL ALTER TABLE dbo.Merchant            DROP CONSTRAINT IF EXISTS FK_Merchant_Address;
-- Address
IF OBJECT_ID('dbo.Address',             'U') IS NOT NULL ALTER TABLE dbo.Address             DROP CONSTRAINT IF EXISTS FK_Address_Zone;
-- Employee
IF OBJECT_ID('dbo.Employee',            'U') IS NOT NULL ALTER TABLE dbo.Employee            DROP CONSTRAINT IF EXISTS FK_Employee_Role;
IF OBJECT_ID('dbo.Employee',            'U') IS NOT NULL ALTER TABLE dbo.Employee            DROP CONSTRAINT IF EXISTS FK_Employee_Supervisor;
GO

-- ============================================================
-- STEP 2: DROP ALL TABLES IN REVERSE DEPENDENCY ORDER
-- Child tables first, parent tables last
-- ============================================================
IF OBJECT_ID('dbo.Performance_Score',  'U') IS NOT NULL DROP TABLE dbo.Performance_Score;
IF OBJECT_ID('dbo.Driver_Payment',     'U') IS NOT NULL DROP TABLE dbo.Driver_Payment;
IF OBJECT_ID('dbo.Feedback',           'U') IS NOT NULL DROP TABLE dbo.Feedback;
IF OBJECT_ID('dbo.Invoice',            'U') IS NOT NULL DROP TABLE dbo.Invoice;
IF OBJECT_ID('dbo.POD_Evidence',       'U') IS NOT NULL DROP TABLE dbo.POD_Evidence;
IF OBJECT_ID('dbo.Shipment_Attempt',   'U') IS NOT NULL DROP TABLE dbo.Shipment_Attempt;
IF OBJECT_ID('dbo.Shipment_Product',   'U') IS NOT NULL DROP TABLE dbo.Shipment_Product;
IF OBJECT_ID('dbo.Shipment_Status',    'U') IS NOT NULL DROP TABLE dbo.Shipment_Status;
IF OBJECT_ID('dbo.Shipment',           'U') IS NOT NULL DROP TABLE dbo.Shipment;
IF OBJECT_ID('dbo.Driver_Route',       'U') IS NOT NULL DROP TABLE dbo.Driver_Route;
IF OBJECT_ID('dbo.Route_Stops',        'U') IS NOT NULL DROP TABLE dbo.Route_Stops;
IF OBJECT_ID('dbo.Route',              'U') IS NOT NULL DROP TABLE dbo.Route;
IF OBJECT_ID('dbo.Driver_Vehicle',     'U') IS NOT NULL DROP TABLE dbo.Driver_Vehicle;
IF OBJECT_ID('dbo.Vehicle',            'U') IS NOT NULL DROP TABLE dbo.Vehicle;
IF OBJECT_ID('dbo.Driver',             'U') IS NOT NULL DROP TABLE dbo.Driver;
IF OBJECT_ID('dbo.Product_Warehouse',  'U') IS NOT NULL DROP TABLE dbo.Product_Warehouse;
IF OBJECT_ID('dbo.Product',            'U') IS NOT NULL DROP TABLE dbo.Product;
IF OBJECT_ID('dbo.Warehouse',          'U') IS NOT NULL DROP TABLE dbo.Warehouse;
IF OBJECT_ID('dbo.Merchant',           'U') IS NOT NULL DROP TABLE dbo.Merchant;
IF OBJECT_ID('dbo.Address',            'U') IS NOT NULL DROP TABLE dbo.Address;
IF OBJECT_ID('dbo.Zone',               'U') IS NOT NULL DROP TABLE dbo.Zone;
IF OBJECT_ID('dbo.Employee',           'U') IS NOT NULL DROP TABLE dbo.Employee;
IF OBJECT_ID('dbo.Role',               'U') IS NOT NULL DROP TABLE dbo.Role;
GO

-- ============================================================
-- STEP 3: CREATE ALL TABLES
-- ============================================================

-- ============================================================
-- 1. ROLE
-- ============================================================
CREATE TABLE dbo.Role (
    role_id                 INT             NOT NULL IDENTITY(1,1),
    role_name               NVARCHAR(100)   NOT NULL,
    permissions_description NVARCHAR(MAX)   NULL,
    CONSTRAINT PK_Role PRIMARY KEY (role_id)
);
GO

-- ============================================================
-- 2. EMPLOYEE
-- ============================================================
CREATE TABLE dbo.Employee (
    employee_id     INT             NOT NULL IDENTITY(1,1),
    name            NVARCHAR(150)   NOT NULL,
    email           NVARCHAR(255)   NOT NULL,
    password        NVARCHAR(255)   NOT NULL,
    account_status  NVARCHAR(50)    NOT NULL DEFAULT 'active',
    role_id         INT             NULL,
    supervisor_id   INT             NULL,
    CONSTRAINT PK_Employee              PRIMARY KEY (employee_id),
    CONSTRAINT UQ_Employee_Email        UNIQUE      (email),
    CONSTRAINT FK_Employee_Role         FOREIGN KEY (role_id)
        REFERENCES dbo.Role (role_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Employee_Supervisor   FOREIGN KEY (supervisor_id)
        REFERENCES dbo.Employee (employee_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 3. ZONE
-- ============================================================
CREATE TABLE dbo.Zone (
    zone_id             INT             NOT NULL IDENTITY(1,1),
    name                NVARCHAR(100)   NOT NULL,
    city                NVARCHAR(100)   NULL,
    geojson_boundary    NVARCHAR(MAX)   NULL,
    delivery_fee        DECIMAL(10,2)   NULL DEFAULT 0.00,
    active_status       BIT             NOT NULL DEFAULT 1,
    CONSTRAINT PK_Zone PRIMARY KEY (zone_id)
);
GO

-- ============================================================
-- 4. ADDRESS
-- ============================================================
CREATE TABLE dbo.Address (
    address_id  INT             NOT NULL IDENTITY(1,1),
    -- city removed: transitive dependency (address_id → zone_id → city)
    -- city is already available via JOIN with Zone table
    district    NVARCHAR(100)   NULL,
    street      NVARCHAR(255)   NULL,
    latitude    DECIMAL(10,8)   NULL,
    longitude   DECIMAL(11,8)   NULL,
    zone_id     INT             NULL,
    CONSTRAINT PK_Address       PRIMARY KEY (address_id),
    CONSTRAINT FK_Address_Zone  FOREIGN KEY (zone_id)
        REFERENCES dbo.Zone (zone_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO

-- ============================================================
-- 5. MERCHANT
-- ============================================================
CREATE TABLE dbo.Merchant (
    merchant_id     INT             NOT NULL IDENTITY(1,1),
    shop_name       NVARCHAR(150)   NOT NULL,
    email           NVARCHAR(255)   NOT NULL,
    password        NVARCHAR(255)   NOT NULL,
    contact_name    NVARCHAR(150)   NULL,
    phone           NVARCHAR(30)    NULL,
    status          NVARCHAR(50)    NOT NULL DEFAULT 'active',
    address_id      INT             NULL,
    CONSTRAINT PK_Merchant          PRIMARY KEY (merchant_id),
    CONSTRAINT UQ_Merchant_Email    UNIQUE      (email),
    CONSTRAINT FK_Merchant_Address  FOREIGN KEY (address_id)
        REFERENCES dbo.Address (address_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO

-- ============================================================
-- 6. WAREHOUSE
-- ============================================================
CREATE TABLE dbo.Warehouse (
    warehouse_id    INT             NOT NULL IDENTITY(1,1),
    warehouse_name  NVARCHAR(150)   NOT NULL,
    address_id      INT             NULL,
    CONSTRAINT PK_Warehouse             PRIMARY KEY (warehouse_id),
    CONSTRAINT FK_Warehouse_Address     FOREIGN KEY (address_id)
        REFERENCES dbo.Address (address_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO

-- ============================================================
-- 7. PRODUCT
-- ============================================================
CREATE TABLE dbo.Product (
    product_id      INT             NOT NULL IDENTITY(1,1),
    name            NVARCHAR(150)   NOT NULL,
    description     NVARCHAR(MAX)   NULL,
    price           DECIMAL(10,2)   NOT NULL,
    stock_quantity  INT             NOT NULL DEFAULT 0,
    merchant_id     INT             NULL,
    CONSTRAINT PK_Product           PRIMARY KEY (product_id),
    CONSTRAINT FK_Product_Merchant  FOREIGN KEY (merchant_id)
        REFERENCES dbo.Merchant (merchant_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO

-- ============================================================
-- 8. PRODUCT_WAREHOUSE  (M:M)
-- ============================================================
CREATE TABLE dbo.Product_Warehouse (
    product_id      INT     NOT NULL,
    warehouse_id    INT     NOT NULL,
    CONSTRAINT PK_Product_Warehouse PRIMARY KEY (product_id, warehouse_id),
    CONSTRAINT FK_PW_Product        FOREIGN KEY (product_id)
        REFERENCES dbo.Product (product_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_PW_Warehouse      FOREIGN KEY (warehouse_id)
        REFERENCES dbo.Warehouse (warehouse_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 9. DRIVER
-- ============================================================
CREATE TABLE dbo.Driver (
    driver_id               INT             NOT NULL IDENTITY(1,1),
    name                    NVARCHAR(150)   NOT NULL,
    phone_number            NVARCHAR(30)    NULL,
    license_number          NVARCHAR(50)    NULL,
    license_expiry_date     DATE            NULL,
    current_status          NVARCHAR(50)    NOT NULL DEFAULT 'available',
    zone_id                 INT             NULL,
    address_id              INT             NULL,
    employee_id             INT             NULL,
    CONSTRAINT PK_Driver            PRIMARY KEY (driver_id),
    CONSTRAINT FK_Driver_Zone       FOREIGN KEY (zone_id)
        REFERENCES dbo.Zone (zone_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Driver_Address    FOREIGN KEY (address_id)
        REFERENCES dbo.Address (address_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Driver_Employee   FOREIGN KEY (employee_id)
        REFERENCES dbo.Employee (employee_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO

-- ============================================================
-- 10. VEHICLE
-- ============================================================
CREATE TABLE dbo.Vehicle (
    vehicle_id              INT             NOT NULL IDENTITY(1,1),
    license_plate           NVARCHAR(30)    NOT NULL,
    model                   NVARCHAR(100)   NULL,
    vehicle_type            NVARCHAR(50)    NULL,
    max_capacity_kg         DECIMAL(8,2)    NULL,
    license_expiry_date     DATE            NULL,
    CONSTRAINT PK_Vehicle       PRIMARY KEY (vehicle_id),
    CONSTRAINT UQ_Vehicle_Plate UNIQUE      (license_plate)
);
GO

-- ============================================================
-- 11. DRIVER_VEHICLE  (M:M)
-- ============================================================
CREATE TABLE dbo.Driver_Vehicle (
    driver_id   INT     NOT NULL,
    vehicle_id  INT     NOT NULL,
    CONSTRAINT PK_Driver_Vehicle    PRIMARY KEY (driver_id, vehicle_id),
    CONSTRAINT FK_DV_Driver         FOREIGN KEY (driver_id)
        REFERENCES dbo.Driver (driver_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_DV_Vehicle        FOREIGN KEY (vehicle_id)
        REFERENCES dbo.Vehicle (vehicle_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 12. ROUTE
-- ============================================================
CREATE TABLE dbo.Route (
    route_id    INT             NOT NULL IDENTITY(1,1),
    route_name  NVARCHAR(150)   NULL,
    status      NVARCHAR(50)    NOT NULL DEFAULT 'active',
    CONSTRAINT PK_Route PRIMARY KEY (route_id)
);
GO

-- ============================================================
-- 13. ROUTE_STOPS  (Weak entity)
-- ============================================================
CREATE TABLE dbo.Route_Stops (
    stop_id         INT         NOT NULL IDENTITY(1,1),
    route_id        INT         NOT NULL,
    address_id      INT         NOT NULL,
    sequence_order  INT         NOT NULL,
    delivered_at    DATETIME2   NULL,
    CONSTRAINT PK_Route_Stops   PRIMARY KEY (stop_id),
    CONSTRAINT FK_RS_Route      FOREIGN KEY (route_id)
        REFERENCES dbo.Route (route_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_RS_Address    FOREIGN KEY (address_id)
        REFERENCES dbo.Address (address_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 14. DRIVER_ROUTE  (assigned_to)
-- ============================================================
CREATE TABLE dbo.Driver_Route (
    driver_id   INT             NOT NULL,
    route_id    INT             NOT NULL,
    route_pay   DECIMAL(10,2)   NULL DEFAULT 0.00,
    CONSTRAINT PK_Driver_Route  PRIMARY KEY (driver_id, route_id),
    CONSTRAINT FK_DR_Driver     FOREIGN KEY (driver_id)
        REFERENCES dbo.Driver (driver_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_DR_Route      FOREIGN KEY (route_id)
        REFERENCES dbo.Route (route_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 15. SHIPMENT
-- ============================================================
CREATE TABLE dbo.Shipment (
    shipment_id     INT             NOT NULL IDENTITY(1,1),
    creation_date   DATETIME2       NOT NULL DEFAULT GETDATE(),
    cod_amount      DECIMAL(10,2)   NOT NULL DEFAULT 0.00,
    weight_kg       DECIMAL(8,2)    NULL,
    warehouse_id    INT             NULL,
    merchant_id     INT             NULL,
    route_id        INT             NULL,
    address_id      INT             NULL,
    CONSTRAINT PK_Shipment              PRIMARY KEY (shipment_id),
    CONSTRAINT FK_Shipment_Warehouse    FOREIGN KEY (warehouse_id)
        REFERENCES dbo.Warehouse (warehouse_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Shipment_Merchant     FOREIGN KEY (merchant_id)
        REFERENCES dbo.Merchant (merchant_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION,
    CONSTRAINT FK_Shipment_Route        FOREIGN KEY (route_id)
        REFERENCES dbo.Route (route_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Shipment_Address      FOREIGN KEY (address_id)
        REFERENCES dbo.Address (address_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 16. SHIPMENT_STATUS  (status history)
-- ============================================================
CREATE TABLE dbo.Shipment_Status (
    status_id   INT             NOT NULL IDENTITY(1,1),
    shipment_id INT             NOT NULL,
    status      NVARCHAR(100)   NOT NULL,
    updated_at  DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Shipment_Status   PRIMARY KEY (status_id),
    CONSTRAINT FK_SS_Shipment       FOREIGN KEY (shipment_id)
        REFERENCES dbo.Shipment (shipment_id)
        ON DELETE CASCADE ON UPDATE CASCADE
);
GO

-- ============================================================
-- 17. SHIPMENT_PRODUCT  (M:M)
-- ============================================================
CREATE TABLE dbo.Shipment_Product (
    shipment_id INT     NOT NULL,
    product_id  INT     NOT NULL,
    quantity    INT     NOT NULL DEFAULT 1,
    CONSTRAINT PK_Shipment_Product  PRIMARY KEY (shipment_id, product_id),
    CONSTRAINT FK_SP_Shipment       FOREIGN KEY (shipment_id)
        REFERENCES dbo.Shipment (shipment_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_SP_Product        FOREIGN KEY (product_id)
        REFERENCES dbo.Product (product_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 18. SHIPMENT_ATTEMPT  (Weak entity)
-- ============================================================
CREATE TABLE dbo.Shipment_Attempt (
    attempt_id      INT             NOT NULL IDENTITY(1,1),
    shipment_id     INT             NOT NULL,
    driver_id       INT             NULL,
    attempt_number  INT             NOT NULL,
    status          NVARCHAR(50)    NULL,
    otp_code        NVARCHAR(20)    NULL,
    photo_url       NVARCHAR(500)   NULL,
    reason          NVARCHAR(255)   NULL,
    attempt_time    DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Shipment_Attempt  PRIMARY KEY (attempt_id),
    CONSTRAINT FK_Attempt_Shipment  FOREIGN KEY (shipment_id)
        REFERENCES dbo.Shipment (shipment_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT FK_Attempt_Driver    FOREIGN KEY (driver_id)
        REFERENCES dbo.Driver (driver_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION  -- Avoids cascade cycle
);
GO

-- ============================================================
-- 19. POD_EVIDENCE  (1:1 with Shipment_Attempt)
-- ============================================================
CREATE TABLE dbo.POD_Evidence (
    attempt_id   INT             NOT NULL,
    photo_url    NVARCHAR(500)   NULL,
    otp_verified BIT             NOT NULL DEFAULT 0,
    notes        NVARCHAR(MAX)   NULL,
    CONSTRAINT PK_POD_Evidence  PRIMARY KEY (attempt_id),
    CONSTRAINT FK_POD_Attempt   FOREIGN KEY (attempt_id)
        REFERENCES dbo.Shipment_Attempt (attempt_id)
        ON DELETE CASCADE ON UPDATE CASCADE
);
GO

-- ============================================================
-- 20. INVOICE
-- ============================================================
CREATE TABLE dbo.Invoice (
    invoice_id      INT             NOT NULL IDENTITY(1,1),
    shipment_id     INT             NULL,
    merchant_id     INT             NULL,
    amount_due      DECIMAL(10,2)   NOT NULL,
    payment_status  NVARCHAR(50)    NOT NULL DEFAULT 'pending',
    period_start    DATE            NULL,
    period_end      DATE            NULL,
    CONSTRAINT PK_Invoice           PRIMARY KEY (invoice_id),
    CONSTRAINT FK_Invoice_Shipment  FOREIGN KEY (shipment_id)
        REFERENCES dbo.Shipment (shipment_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Invoice_Merchant  FOREIGN KEY (merchant_id)
        REFERENCES dbo.Merchant (merchant_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION
);
GO

-- ============================================================
-- 21. FEEDBACK
-- ============================================================
CREATE TABLE dbo.Feedback (
    feedback_id INT             NOT NULL IDENTITY(1,1),
    shipment_id INT             NULL,
    driver_id   INT             NULL,
    rating      DECIMAL(3,1)    NULL,
    comment     NVARCHAR(MAX)   NULL,
    created_at  DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Feedback          PRIMARY KEY (feedback_id),
    CONSTRAINT FK_Feedback_Shipment FOREIGN KEY (shipment_id)
        REFERENCES dbo.Shipment (shipment_id)
        ON DELETE SET NULL ON UPDATE CASCADE,
    CONSTRAINT FK_Feedback_Driver   FOREIGN KEY (driver_id)
        REFERENCES dbo.Driver (driver_id)
        ON DELETE NO ACTION ON UPDATE NO ACTION  -- Avoids cascade cycle
);
GO

-- ============================================================
-- 23. DRIVER_PAYMENT
-- ============================================================
CREATE TABLE dbo.Driver_Payment (
    payment_id      INT             NOT NULL IDENTITY(1,1),
    driver_id       INT             NULL,
    total_payout    DECIMAL(10,2)   NULL DEFAULT 0.00,
    distance_km     DECIMAL(10,2)   NULL DEFAULT 0.00,
    created_at      DATETIME2       NOT NULL DEFAULT GETDATE(),
    -- completed_deliveries_count removed: transitive dependency via driver_id
    -- derive it with: SELECT COUNT(*) FROM Shipment_Attempt WHERE driver_id = x AND status = 'delivered'
    CONSTRAINT PK_Driver_Payment    PRIMARY KEY (payment_id),
    CONSTRAINT FK_DP_Driver         FOREIGN KEY (driver_id)
        REFERENCES dbo.Driver (driver_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO

-- ============================================================
-- 24. PERFORMANCE_SCORE  (AI)
-- ============================================================
CREATE TABLE dbo.Performance_Score (
    perf_id             INT             NOT NULL IDENTITY(1,1),
    driver_id           INT             NULL,
    daily_score         DECIMAL(5,2)    NULL,
    tier                NVARCHAR(50)    NULL,
    weighted_history    DECIMAL(5,2)    NULL,
    recorded_at         DATETIME2       NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_Performance_Score PRIMARY KEY (perf_id),
    CONSTRAINT FK_PS_Driver         FOREIGN KEY (driver_id)
        REFERENCES dbo.Driver (driver_id)
        ON DELETE SET NULL ON UPDATE CASCADE
);
GO