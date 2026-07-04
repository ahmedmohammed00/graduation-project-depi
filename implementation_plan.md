# FastDrop — ASP.NET Core Web API Implementation Plan

## Overview

FastDrop is a **Delivery Management System (DMS)** for last-mile courier/logistics operations. The existing project contains:
- **25 EF Core domain models** (ConsoleApp1) with a full SQL Server schema
- **12 HTML UI pages** covering Admin, Dispatcher, Driver, and Merchant roles

This plan defines how to build a **clean ASP.NET Core 8 Web API** that powers all 12 UI pages, re-using the existing EF Core data layer.

---

## Existing Domain — What Was Found

### Entity Summary (25 Models)

| Group | Entities |
|---|---|
| **Identity** | `Role`, `Employee`, `Merchant` |
| **Drivers** | `Driver`, `Vehicle`, `DriverVehicle`, `DriverRoute`, `DriverPayment`, `PerformanceScore` |
| **Geography** | `Zone`, `Address`, `Warehouse` |
| **Routing** | `Route`, `RouteStop` |
| **Shipments** | `Shipment`, `ShipmentStatus`, `ShipmentAttempt`, `PodEvidence`, `ShipmentProduct` |
| **Products** | `Product`, `ProductWarehouse` |
| **Finance** | `Invoice` |
| **AI / Analytics** | `Feedback`, `SentimentAnalysis`, `DemandForecast` |

### Key Business Logic (from DbContext relationships)

- **Employee** has a self-referencing supervisor hierarchy (`SupervisorId`)
- **Driver** is managed by an `Employee` and assigned to a `Zone`
- **Shipment** tracks lifecycle via `ShipmentStatus` history (Pending → In-Transit → Delivered/Failed)
- **ShipmentAttempt** stores each delivery try with OTP code, photo URL, reason
- **PodEvidence** is a 1-to-1 with `ShipmentAttempt` (proof-of-delivery)
- **Invoice** is per-shipment billed to a Merchant with period dates
- **PerformanceScore** tracks driver's daily score / tier / weighted history
- **DemandForecast** gives per-zone predicted volume with peak flag

---

## Architecture

```
FastDrop.API/
├── Controllers/           ← HTTP layer (thin, delegates to services)
├── Services/              ← Business logic layer
│   ├── Interfaces/
│   └── Implementations/
├── DTOs/                  ← Request / Response shapes (no model leakage)
│   ├── Requests/
│   └── Responses/
├── Middleware/            ← Global error handler, JWT extractor
├── Extensions/            ← Service registration helpers
├── Program.cs
└── appsettings.json

FastDrop.Data/             ← Moved from ConsoleApp1 (re-used as-is)
├── Data/DmsDbContext.cs
└── Models/  (all 25 models)
```

> **Pattern**: Repository-free. Controllers call `IXxxService`, services call `DmsDbContext` directly (EF Core is already the abstraction). Simple and fast to build.

---

## Proposed Changes

### Phase 1 — Project Scaffold

#### [NEW] `FastDrop.API` — ASP.NET Core Web API project
Created alongside ConsoleApp1 in the same solution.

**NuGet packages:**
```
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.AspNetCore.Authentication.JwtBearer
BCrypt.Net-Next
Swashbuckle.AspNetCore
AutoMapper.Extensions.Microsoft.DependencyInjection
```

**`appsettings.json`** — connection string + JWT config:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=dms_db;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "FastDropAPI",
    "Audience": "FastDropClient",
    "ExpiryMinutes": 60
  }
}
```

---

### Phase 2 — Auth Module

**UI pages served:** `fastdrop-login.html`, `fastdrop-signup.html`, `fastdrop-forgot-password.html`

#### [NEW] `Controllers/AuthController.cs`

| Endpoint | Method | Description |
|---|---|---|
| `POST /api/auth/login` | Public | Employee OR Merchant login → returns JWT |
| `POST /api/auth/signup` | Public | Merchant self-registration |
| `POST /api/auth/forgot-password` | Public | Send reset token (email stub) |
| `POST /api/auth/reset-password` | Public | Consume reset token → update password |

**JWT Payload:**
```json
{ "sub": "1", "role": "Admin", "type": "employee", "name": "John" }
```

**DTOs:**
- `LoginRequest { Email, Password, UserType }`
- `LoginResponse { Token, ExpiresAt, Name, Role }`
- `SignupRequest { ShopName, Email, Password, ContactName, Phone }`

---

### Phase 3 — Core CRUD Controllers

Each controller follows the same pattern:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class XxxController : ControllerBase
```

#### [NEW] `Controllers/RolesController.cs`
**UI:** `fastdrop-user-roles.html`

| Endpoint | Method |
|---|---|
| `GET /api/roles` | Get all roles |
| `GET /api/roles/{id}` | Get single |
| `POST /api/roles` | Create |
| `PUT /api/roles/{id}` | Update |
| `DELETE /api/roles/{id}` | Delete |

---

#### [NEW] `Controllers/EmployeesController.cs`
**UI:** `fastdrop-user-roles.html`, `fastdrop-admin-dashboard.html`

| Endpoint | Method |
|---|---|
| `GET /api/employees` | List with role + supervisor |
| `GET /api/employees/{id}` | Single |
| `POST /api/employees` | Create |
| `PUT /api/employees/{id}` | Update |
| `PATCH /api/employees/{id}/status` | Activate/deactivate |
| `DELETE /api/employees/{id}` | Delete |

---

#### [NEW] `Controllers/DriversController.cs`
**UI:** `fastdrop-drivers.html`

| Endpoint | Method |
|---|---|
| `GET /api/drivers` | List with zone + status filter |
| `GET /api/drivers/{id}` | Single with vehicles + routes |
| `POST /api/drivers` | Create |
| `PUT /api/drivers/{id}` | Update |
| `PATCH /api/drivers/{id}/status` | Change status (available/busy/offline) |
| `GET /api/drivers/{id}/performance` | Performance scores history |
| `GET /api/drivers/{id}/payments` | Payment history |
| `POST /api/drivers/{id}/vehicles/{vehicleId}` | Assign vehicle |
| `DELETE /api/drivers/{id}/vehicles/{vehicleId}` | Unassign vehicle |

---

#### [NEW] `Controllers/MerchantsController.cs`
**UI:** `fastdrop-merchants.html`

| Endpoint | Method |
|---|---|
| `GET /api/merchants` | List with status filter |
| `GET /api/merchants/{id}` | Single with shipment count + invoices |
| `POST /api/merchants` | Create |
| `PUT /api/merchants/{id}` | Update |
| `PATCH /api/merchants/{id}/status` | Activate/suspend |
| `DELETE /api/merchants/{id}` | Delete |

---

#### [NEW] `Controllers/VehiclesController.cs`

| Endpoint | Method |
|---|---|
| `GET /api/vehicles` | List |
| `GET /api/vehicles/{id}` | Single |
| `POST /api/vehicles` | Create |
| `PUT /api/vehicles/{id}` | Update |
| `DELETE /api/vehicles/{id}` | Delete |

---

#### [NEW] `Controllers/ZonesController.cs`

| Endpoint | Method |
|---|---|
| `GET /api/zones` | List all zones |
| `GET /api/zones/{id}` | Single with driver count |
| `POST /api/zones` | Create |
| `PUT /api/zones/{id}` | Update |
| `DELETE /api/zones/{id}` | Delete |
| `GET /api/zones/{id}/forecasts` | Demand forecasts for zone |

---

#### [NEW] `Controllers/WarehousesController.cs`

| Endpoint | Method |
|---|---|
| `GET /api/warehouses` | List |
| `GET /api/warehouses/{id}` | Single with product inventory |
| `POST /api/warehouses` | Create |
| `PUT /api/warehouses/{id}` | Update |
| `DELETE /api/warehouses/{id}` | Delete |

---

#### [NEW] `Controllers/RoutesController.cs`
**UI:** `fastdrop-dispatcher.html`

| Endpoint | Method |
|---|---|
| `GET /api/routes` | List with status filter |
| `GET /api/routes/{id}` | Single with stops + assigned drivers |
| `POST /api/routes` | Create route |
| `PUT /api/routes/{id}` | Update |
| `DELETE /api/routes/{id}` | Delete |
| `POST /api/routes/{id}/stops` | Add stop |
| `DELETE /api/routes/{id}/stops/{stopId}` | Remove stop |
| `POST /api/routes/{id}/drivers/{driverId}` | Assign driver |
| `DELETE /api/routes/{id}/drivers/{driverId}` | Remove driver |

---

### Phase 4 — Shipments Module (Core Business Logic)

**UI:** `fastdrop-shipments.html`, `fastdrop-dispatcher.html`

#### [NEW] `Controllers/ShipmentsController.cs`

| Endpoint | Method | Notes |
|---|---|---|
| `GET /api/shipments` | List | Filters: status, merchantId, driverId, dateRange |
| `GET /api/shipments/{id}` | Single | Full detail with status history + attempts |
| `POST /api/shipments` | Create | Auto-creates first `ShipmentStatus = "Pending"` |
| `PUT /api/shipments/{id}` | Update | Edit COD, weight, address, etc. |
| `DELETE /api/shipments/{id}` | Delete | Cascade removes statuses + attempts |
| `PATCH /api/shipments/{id}/status` | Update status | Appends to ShipmentStatus history |
| `GET /api/shipments/{id}/attempts` | Get all attempts | With POD evidence |
| `POST /api/shipments/{id}/attempts` | Create attempt | Records OTP, photo, driver |
| `POST /api/shipments/{id}/attempts/{attemptId}/pod` | Upload POD | Creates PodEvidence |

**Status flow enforced in service layer:**
```
Pending → In-Transit → [Delivered | Failed | Returned]
```

---

### Phase 5 — Products & Inventory

#### [NEW] `Controllers/ProductsController.cs`

| Endpoint | Method |
|---|---|
| `GET /api/products` | List (filter by merchantId) |
| `GET /api/products/{id}` | Single |
| `POST /api/products` | Create |
| `PUT /api/products/{id}` | Update stock/price |
| `DELETE /api/products/{id}` | Delete |
| `GET /api/products/{id}/warehouses` | See stock per warehouse |

---

### Phase 6 — Finance Module

**UI:** `fastdrop-invoices.html`

#### [NEW] `Controllers/InvoicesController.cs`

| Endpoint | Method |
|---|---|
| `GET /api/invoices` | List (filter: merchantId, status, period) |
| `GET /api/invoices/{id}` | Single |
| `POST /api/invoices` | Create |
| `PUT /api/invoices/{id}` | Update |
| `PATCH /api/invoices/{id}/status` | Mark paid/unpaid/overdue |
| `DELETE /api/invoices/{id}` | Delete |

---

### Phase 7 — Feedback & AI Analytics

**UI:** `fastdrop-analytics.html`

#### [NEW] `Controllers/FeedbackController.cs`

| Endpoint | Method |
|---|---|
| `GET /api/feedback` | List (filter: driverId, shipmentId) |
| `POST /api/feedback` | Submit feedback |
| `GET /api/feedback/{id}/sentiment` | Get sentiment analysis |

#### [NEW] `Controllers/AnalyticsController.cs`

| Endpoint | Method | Returns |
|---|---|---|
| `GET /api/analytics/dashboard` | Admin KPIs | Total shipments, drivers on duty, revenue, pending count |
| `GET /api/analytics/driver-performance` | Driver ranking | Top/bottom drivers by score + tier |
| `GET /api/analytics/demand-forecasts` | Zone forecasts | Volume + peak flag per zone |
| `GET /api/analytics/shipment-trends` | Time series | Daily/weekly delivery volume |
| `GET /api/analytics/revenue` | Finance summary | Revenue by period + by merchant |

---

### Phase 8 — Cross-Cutting Concerns

#### [NEW] `Middleware/GlobalExceptionMiddleware.cs`
Catches all unhandled exceptions, returns structured JSON:
```json
{ "error": "Internal Server Error", "detail": "...", "traceId": "..." }
```

#### [NEW] `Extensions/ServiceCollectionExtensions.cs`
Registers: DbContext, JWT auth, AutoMapper, Swagger, CORS, all services.

#### [NEW] `Program.cs`
```csharp
builder.Services.AddFastDropServices(builder.Configuration);
// CORS for UI HTML files
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GlobalExceptionMiddleware>();
app.MapControllers();
```

#### [NEW] Swagger Setup
- Full JWT bearer support in Swagger UI
- All endpoints documented with XML comments
- Available at `/swagger`

---

## DTOs Strategy

All API inputs/outputs use DTOs — **never expose EF models directly**.

**Example — ShipmentResponseDto:**
```csharp
public record ShipmentResponseDto(
    int ShipmentId,
    DateTime CreationDate,
    decimal CodAmount,
    decimal? WeightKg,
    string CurrentStatus,     // latest ShipmentStatus.Status
    string MerchantName,
    string WarehouseName,
    string RouteN ame,
    string DeliveryAddress
);
```

**Example — CreateShipmentRequest:**
```csharp
public record CreateShipmentRequest(
    decimal CodAmount,
    decimal? WeightKg,
    int? MerchantId,
    int? WarehouseId,
    int? RouteId,
    int? AddressId
);
```

---

## Authorization Matrix

| Role | Accessible Endpoints |
|---|---|
| **Admin** | All endpoints |
| **Dispatcher** | Shipments, Routes, Drivers (assign), Analytics read |
| **Driver** | Own shipment attempts, own performance |
| **Merchant** | Own shipments, own invoices, own products, feedback submit |

Enforced via `[Authorize(Roles = "Admin,Dispatcher")]` on controllers/actions.

---

## Project File Layout (Final)

```
c:\Users\godab\Desktop\fast-drop\
├── Fast-drop-master\
│   └── ConsoleApp1\               ← existing data layer (kept as-is)
│       ├── Models\ (25 models)
│       ├── Data\DmsDbContext.cs
│       └── Migrations\
│
└── FastDrop.API\                  ← NEW: Web API project
    ├── FastDrop.API.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── Controllers\
    │   ├── AuthController.cs
    │   ├── RolesController.cs
    │   ├── EmployeesController.cs
    │   ├── DriversController.cs
    │   ├── MerchantsController.cs
    │   ├── VehiclesController.cs
    │   ├── ZonesController.cs
    │   ├── WarehousesController.cs
    │   ├── RoutesController.cs
    │   ├── ShipmentsController.cs
    │   ├── ProductsController.cs
    │   ├── InvoicesController.cs
    │   ├── FeedbackController.cs
    │   └── AnalyticsController.cs
    ├── Services\
    │   ├── Interfaces\
    │   │   ├── IAuthService.cs
    │   │   ├── IDriverService.cs
    │   │   ├── IShipmentService.cs
    │   │   └── ... (one per controller)
    │   └── Implementations\
    │       ├── AuthService.cs
    │       ├── DriverService.cs
    │       ├── ShipmentService.cs
    │       └── ...
    ├── DTOs\
    │   ├── Requests\
    │   └── Responses\
    ├── Middleware\
    │   └── GlobalExceptionMiddleware.cs
    └── Extensions\
        └── ServiceCollectionExtensions.cs
```

---

## Verification Plan

### Automated Tests
- Run `dotnet build` to verify compilation
- Test all endpoints via Swagger UI at `http://localhost:5000/swagger`
- Verify JWT: login → get token → use Bearer in Swagger → access protected endpoint

### Manual Verification per UI Page

| UI Page | Endpoints to verify |
|---|---|
| `fastdrop-login.html` | `POST /api/auth/login` |
| `fastdrop-signup.html` | `POST /api/auth/signup` |
| `fastdrop-admin-dashboard.html` | `GET /api/analytics/dashboard` |
| `fastdrop-drivers.html` | `GET /api/drivers`, `PATCH /api/drivers/{id}/status` |
| `fastdrop-merchants.html` | `GET /api/merchants`, `PATCH /api/merchants/{id}/status` |
| `fastdrop-shipments.html` | `GET /api/shipments`, `PATCH /api/shipments/{id}/status` |
| `fastdrop-dispatcher.html` | `GET /api/routes`, `POST /api/routes/{id}/drivers/{driverId}` |
| `fastdrop-invoices.html` | `GET /api/invoices`, `PATCH /api/invoices/{id}/status` |
| `fastdrop-analytics.html` | `GET /api/analytics/driver-performance`, `GET /api/analytics/demand-forecasts` |
| `fastdrop-user-roles.html` | `GET /api/roles`, `GET /api/employees` |

---

## Open Questions

> [!IMPORTANT]
> **Q1 — Password reset**: Should "forgot password" actually send an email (requires SMTP config) or just return a reset token in the response for now?

> [!IMPORTANT]
> **Q2 — Sentiment Analysis**: The `SentimentAnalysis` model exists. Should the API call an external AI service (e.g., Azure Cognitive Services) or store manually-entered sentiment values?

> [!IMPORTANT]
> **Q3 — Photo/POD uploads**: `ShipmentAttempt.PhotoUrl` stores a URL. Should the API include a file upload endpoint (multipart) that saves to disk/Azure Blob, or just accept a URL string?

> [!NOTE]
> **Q4 — Solution structure**: Should `FastDrop.API` be added to the existing `ConsoleApp1.slnx` solution, or create a new separate solution file?
