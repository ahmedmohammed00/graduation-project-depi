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

Merchant shipment and product edits load the persisted resource within the signed-in
merchant's scope. Owner IDs, assignment fields and entity graphs are not accepted by
the edit forms. Shipment address changes create a separate address record so shared
addresses belonging to another merchant, warehouse or route are preserved.

Merchant shipment creation accepts destination details, product IDs and quantities.
The server checks active zone pricing, product ownership and available stock, then
calculates COD from stored prices. Stock reservations, shipment items, captured unit
prices and the initial Pending status commit in one transaction. Optimistic stock,
price and ownership checks reject concurrent stale writes. Invalid or conflicting
requests leave no partial order; pending unassigned shipment deletion releases its
new reservations. The legacy merchant `/Shipments/Create` form redirects to this
workflow, and its POST is restricted to staff.

Deployment requires the additive **`20261115104530_CaptureShipmentUnitPrices`**
migration before running the updated application. Existing line prices remain null
because their historical values cannot be reconstructed reliably. Merchants can
update an older shipment's address within its current zone; zone changes for those
shipments need staff review. New shipments use saved item prices when changing zones.
The migration was tested as an upgrade from `AddDirectDriverToShipment` on an
isolated SQL database; no existing application database was modified.

| Portal | URL |
|--------|-----|
| Home / Landing | `/` |
| Login | `/Account/Login` |
| My profile | `/Profile` |
| Admin Dashboard | `/AdminPortal/Dashboard` |
| Merchant Dashboard | `/MerchantPortal/Dashboard` |
| Dispatcher Dashboard | `/DispatcherPortal/Dashboard` |
| Driver Dashboard | `/DriverPortal/Dashboard` |

Signed-in users can open **My profile** from the sidebar or their avatar. Staff can
edit their name and email; linked drivers can also edit their phone number.
Merchants can edit their shop name, contact name, phone and email. Profile updates
resolve the account from the signed-in identity, never from a submitted account ID;
roles, account status and operational assignments remain administrator-managed.

Changing an email requires the current password and checks both account tables for
an existing address. Password changes require the current password and store a new
salted hash. Both changes revoke existing account tokens, clear this browser's
authentication cookie and redirect to login. Editing ordinary profile details
refreshes the displayed account name without extending the JWT.

### Account sessions and request protection

JWTs expire after 30 minutes. Each authenticated request checks the account's
current active status, roles, driver link and security stamp against the database.
Application password, email, role and account-status changes rotate that stamp.
Signing out uses an antiforgery-protected POST and revokes all existing tokens for
that account, including tokens copied to another browser. Previously issued tokens
without the stamp/type claims require a fresh login after upgrading. Schema
changes must be applied before running this application version.

Login and signup POSTs have separate per-source and per-account fixed-window
budgets. Login defaults to 20 requests per source and 5 per account each minute;
signup permits 3 per source and per account every 15 minutes. Existing account IDs
are resolved using the database's email matching rules so alternate spellings that
identify the same account share a budget. Limits return 429 with Retry-After; GET
forms stay available. Non-form submissions are rejected. Configure positive limits
under `AccountRateLimits` using `LoginIpLimit`, `LoginAccountLimit`, `SignupIpLimit`
and `SignupAccountLimit`. These budgets are local to each application process;
scaled deployments also need shared ingress limits and a reviewed trusted-proxy
configuration. Arbitrary forwarding headers do not override the connection IP.

Unsafe MVC requests validate antiforgery tokens by default, including payment
mutations. Role migrations seed role definitions only; they no longer assign
privileges by email or automatically approve employees without roles. For existing
databases where the historical migration already ran, review prior staff grants
against operator-approved employee IDs. Changing the migration source does not
undo existing account approvals.

### Delivery payout integrity

Delivery confirmation records its marker, status, attempt, evidence and payout in
one transaction. Shipment rowversion checks and a unique non-null shipment payout
index prevent parallel retries from paying twice. Retries return the already
recorded result, and delivered shipments cannot be reopened through generic status
updates. Existing inconsistent delivery/payment records require administrator
review. Recipient OTP verification and photo capture remain simulated (audit S09);
this batch protects payout idempotency, not the authenticity of that proof.

Apply **`20261115220909_ProtectSessionsAndDeliveryPayouts`** after the shipment-price
migration. The migration assigns distinct security stamps to existing accounts and
adds the delivery/concurrency fields. It stops with error 51001 if historical
duplicate shipment payouts exist, before changing the schema; it does not delete
financial records. Reconcile those records through the approved finance process
before retrying. No application database migration is performed automatically.

---

## Documentation

Run the automated regression tests from the repository root:

```bash
dotnet test FastDrop.sln
```

The authorization tests run the application in an isolated HTTP test host using
temporary signing and Data Protection keys. They do not require a SQL database.

Additional relational integration tests use SQL Server LocalDB to verify stock and
delivery races, rollback, price snapshots, session stamps and migration behavior.
They are skipped by default. On Windows, create the
dedicated test instance with `SqlLocalDB create FastDropSecurityAudit -s`, set
`FASTDROP_RUN_SQL_TESTS=1`, then run `dotnet test FastDrop.sln`. The tests create
and delete uniquely named `FastDrop_SecurityTests_*` databases only. Alternatively,
set `FASTDROP_SQL_TEST_INSTANCE=MSSQLLocalDB` to host these disposable databases on
the standard local instance. Other server names and existing database names are
not accepted by the test fixture.

For comprehensive project documentation covering architecture, requirements, database design, and more, see [`fastdrop_project_documentation.md`](fastdrop_project_documentation.md).

---

## License

This project was developed as an academic graduation project for the DEPI program.
