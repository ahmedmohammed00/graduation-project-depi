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

### 3. Run the application

```bash
cd FastDrop.MVC
dotnet run --launch-profile http
```

The app will be available at **http://localhost:5258**.

---

## Portal Entry Points

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

For comprehensive project documentation covering architecture, requirements, database design, and more, see [`fastdrop_project_documentation.md`](fastdrop_project_documentation.md).

---

## License

This project was developed as an academic graduation project for the DEPI program.
