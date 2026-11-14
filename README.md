# FastDrop – Delivery Management System (DMS)

> **Graduation Project** — Digital Egypt Pioneers Initiative (DEPI), Software Engineering Track

FastDrop is a comprehensive, web-based Delivery Management System built with **ASP.NET Core MVC**, **Entity Framework Core**, and **Microsoft SQL Server**. It unifies the entire delivery lifecycle—from merchant product registration and stock allocation, through shipment creation and dispatch, to delivery confirmation and financial settlement—into a single, role-aware platform.

---

## Key Features

| Module | Highlights |
|--------|-----------|
| **Multi-Role Portals** | Admin, Merchant, Dispatcher, and Driver portals with strict RBAC data isolation |
| **Inventory Management** | Multi-warehouse stock tracking with per-warehouse allocation |
| **Fleet Management** | Vehicle-driver linkage as a prerequisite for dispatch eligibility |
| **Shipment Lifecycle** | End-to-end tracking with status audit trails |
| **Dynamic Pricing** | Zone-based delivery cost calculation (COD + shipping fees) |
| **Financial Reconciliation** | Automated merchant invoice generation and driver payment tracking |
| **Performance Scoring** | Quantitative driver evaluation (success rate, timeliness, reliability) |
| **Dispatch Engine** | Real-time shipment-to-driver assignment with eligibility checks |

---

## Technology Stack

| Component | Technology |
|-----------|-----------|
| Runtime Framework | .NET 10 (ASP.NET Core MVC) |
| Language | C# |
| ORM | Entity Framework Core (Code-First) |
| Database | Microsoft SQL Server (LocalDB / SQL Server 2019+) |
| Frontend | Razor Views, Custom CSS (Glassmorphism), Vanilla JS |
| Authentication | JWT Bearer with cookie integration |
| Version Control | Git / GitHub |

---

## Project Structure

```
graduation-project-depi/
├── FastDrop.MVC/                    # Main application
│   ├── Controllers/                 # 17 MVC controllers
│   │   ├── AccountController.cs     # Auth (login/signup/logout)
│   │   ├── AdminPortalController.cs # Admin dashboard & management
│   │   ├── MerchantPortalController.cs
│   │   ├── DispatcherPortalController.cs
│   │   ├── DriverPortalController.cs
│   │   └── ...                      # Resource CRUD controllers
│   ├── Models/                      # 25 EF Core entity models + DbContext
│   ├── Services/                    # 14 service interfaces + implementations
│   ├── ViewModels/                  # View-specific data transfer objects
│   ├── DTOs/                        # Data transfer objects
│   ├── Views/                       # Razor views organized by controller
│   │   ├── Shared/                  # Layout files (_AdminLayout, _MerchantLayout, etc.)
│   │   └── ...
│   ├── wwwroot/                     # Static assets (CSS, JS, images, libs)
│   ├── Migrations/                  # EF Core database migrations
│   ├── Program.cs                   # Application entry point & DI configuration
│   └── appsettings.json             # Configuration (connection strings, JWT)
├── ui/                              # Standalone HTML design prototypes
├── DMS.sql                          # Full database schema script
├── dms_db_script.sql                # Lightweight database script
├── fastdrop_project_documentation.md # Detailed project documentation
└── README.md
```

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or later)
- [SQL Server LocalDB](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) or SQL Server 2019+
- Git

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/ahmedmohammed00/graduation-project-depi.git
cd graduation-project-depi
```

### 2. Set up the database

**Option A** — Apply EF Core migrations (recommended):
```bash
cd FastDrop.MVC
dotnet ef database update
```

**Option B** — Run the SQL script directly:
```bash
sqlcmd -S "(localdb)\mssqllocaldb" -i DMS.sql
```

### 3. Configure a private JWT signing key

The application requires `JwtSettings:Key` to be a Base64-encoded, cryptographically
random key containing at least 32 bytes. No signing key is included in source control.
Startup fails if the key is missing, malformed, too short, or matches the previously
published key.

For local development, run the following PowerShell commands from the repository root
to generate a key and save it in the development user-secrets store:

```powershell
$jwtKeyBytes = New-Object byte[] 32
$jwtRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$jwtRng.GetBytes($jwtKeyBytes)
$jwtRng.Dispose()
$jwtSecret = [Convert]::ToBase64String($jwtKeyBytes)
dotnet user-secrets set "JwtSettings:Key" $jwtSecret --project FastDrop.MVC
Remove-Variable jwtSecret, jwtKeyBytes, jwtRng
```

For deployment, supply `JwtSettings__Key` through your hosting platform's secret
configuration. Use a different key for each environment and share the appropriate
key across instances in the same environment. Do not put keys in committed JSON,
launch profiles, or scripts.

Existing deployments must replace the previously published signing key with a new
private key and restart every instance. This invalidates existing JWTs, so users
must log in again. The previous key remains exposed in Git history and must not be
reused. This change does not rotate secrets in an external deployment automatically.

### 4. Run the application

Before upgrading an existing database, stop other application instances, take a
verified database backup, and run the explicit credential migration using that
deployment's connection string and private JWT configuration:

```bash
dotnet run --project FastDrop.MVC --no-launch-profile -- --migrate-legacy-passwords
```

This command hashes legacy plaintext employee and merchant passwords in batches,
preserves recognized hashes, and exits without starting the web server. It is
idempotent and can be rerun after interruption. Credentials that resemble malformed
or unsupported hashes are skipped and require a verified password reset; the
command reports their count and returns exit code 2. It never prints credentials.
The command is not run automatically on startup. Check the configured database
before running it; do not run a database reset script to migrate passwords.

Login accepts recognized hashes only. New credentials use bcrypt with SHA-384
prehashing and the `bcrypt-sha384:` format tag, preserving the full input for the
supported 8–100 character passwords. Existing standard bcrypt hashes remain
valid. Merchant profile edits and employee role edits cannot change passwords.

```bash
cd FastDrop.MVC
dotnet run --launch-profile http
```

The app will be available at **http://localhost:5258**.

---

## Portal Entry Points

Management endpoints require authenticated access with these roles:

| Controller | Allowed roles |
|------------|---------------|
| Drivers, DriverPayments, Products, Zones | SuperAdmin |
| Routes, Performance | SuperAdmin, Dispatcher |
| Invoices | SuperAdmin |

Public signup creates merchant accounts only. Staff and driver access must be
provisioned through an administrator-controlled process; selecting or submitting a
staff role on the public signup endpoint is rejected. Employees without an assigned
role or a driver record cannot log in. A driver record alone grants Driver access,
not Employee privileges. The general Employee role does not grant invoice or
shipment administration; shipment administration requires SuperAdmin or Dispatcher.

For existing deployments, review accounts created through the old public staff
signup, remove unapproved access, and rotate the signing key to invalidate existing
tokens containing historical privileges.

Merchants and drivers use their own portal routes for their data. New endpoints
require authentication by default unless explicitly marked public. The landing
pages, login/signup pages, access-denied page, and static assets remain public.

Shipment detail reads use a shared database query scoped to the authenticated
caller. Merchants can read their own shipments; drivers can read shipments
assigned directly to them or through their assigned route. If a shipment has a
route, that route determines driver access. SuperAdmin and Dispatcher can read
shipments across merchants for operations. Missing, invalid, nonpositive or
duplicate ownership claims deny access. Inaccessible and nonexistent shipment
IDs both return 404.

| Portal | URL |
|--------|-----|
| Home / Landing | `/` |
| Login | `/Account/Login` |
| Admin Dashboard | `/AdminPortal/Dashboard` |
| Merchant Dashboard | `/MerchantPortal/Dashboard` |
| Dispatcher Dashboard | `/DispatcherPortal/Dashboard` |
| Driver Dashboard | `/DriverPortal/Dashboard` |

---

## Documentation

Run the automated regression tests from the repository root:

```bash
dotnet test FastDrop.sln
```

The authorization tests run the application in an isolated HTTP test host using
temporary signing and Data Protection keys. They do not require a SQL database.

For comprehensive project documentation covering architecture, requirements, database design, and more, see [`fastdrop_project_documentation.md`](fastdrop_project_documentation.md).

---

## License

This project was developed as an academic graduation project for the DEPI program.
