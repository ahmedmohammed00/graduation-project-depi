# FastDrop: A Comprehensive Delivery Management System

**Graduation Project Documentation**

---

**Project Title:** FastDrop – Delivery Management System (DMS)

**Program:** Digital Egypt Pioneers Initiative (DEPI) – Software Engineering Track

**Technology Stack:** ASP.NET Core MVC, Entity Framework Core, SQL Server

**Repository:** github.com/ahmedmohammed00/graduation-project-depi

---

---

# Abstract

The rapid growth of e-commerce and on-demand delivery services has created a pressing need for integrated logistics management platforms. Traditional delivery workflows, often reliant on manual coordination, spreadsheets, and fragmented communication channels, introduce significant inefficiencies in fleet utilization, order fulfillment accuracy, and financial reconciliation.

**FastDrop** is a comprehensive, web-based Delivery Management System (DMS) designed to address these challenges. Built using the ASP.NET Core MVC framework with Entity Framework Core and Microsoft SQL Server, the system provides a centralized platform that unifies four critical stakeholder roles—System Administrators, Merchants, Dispatchers, and Drivers—under a single, cohesive application.

Key capabilities of the system include: multi-warehouse inventory management with per-warehouse stock tracking; an intelligent fleet management module that enforces vehicle-driver linkage as a prerequisite for dispatch eligibility; dynamic zone-based delivery pricing; end-to-end shipment lifecycle tracking with status audit trails; automated merchant invoice generation; driver payment management; and a quantitative driver performance scoring engine.

The system employs a Role-Based Access Control (RBAC) architecture to enforce strict data isolation between user roles, ensuring that each stakeholder interacts exclusively with functionality and data relevant to their operational responsibilities. The user interface is built with a modern, responsive design language featuring custom CSS with glassmorphism aesthetics, data-rich tables, and intuitive navigation structures tailored to each portal.

This document presents the system's architecture, functional and non-functional requirements, database schema design, and directions for future enhancement.

---

# 1. Introduction

## 1.1 Problem Statement

The logistics and last-mile delivery sector faces several persistent operational challenges:

- **Fragmented Coordination:** Merchants, warehouse operators, dispatchers, and drivers frequently operate within disconnected systems, leading to communication breakdowns, delayed shipments, and inaccurate delivery estimates.
- **Manual Fleet Management:** Without a centralized system, tracking vehicle availability, driver assignments, and route optimization relies on manual processes that are error-prone and do not scale.
- **Inventory Opacity:** Merchants who distribute stock across multiple warehouse locations lack a unified view of their inventory, making it difficult to determine product availability and optimal fulfillment sources.
- **Financial Reconciliation Delays:** Generating invoices for merchants based on completed deliveries and calculating driver payouts based on routes performed are labor-intensive processes when handled manually.
- **Absence of Performance Metrics:** Without quantitative performance tracking, organizations lack the data-driven insights necessary to incentivize driver reliability and identify operational bottlenecks.

## 1.2 Proposed Solution

FastDrop is a web-based Delivery Management System that provides an integrated, role-aware platform addressing each of the challenges identified above. The system consolidates the entire delivery lifecycle—from merchant product registration and stock allocation, through shipment creation and dispatch, to delivery confirmation and financial settlement—into a single application.

The platform is architected around four distinct user portals, each tailored to the specific workflow requirements of its target audience:

1. **Admin Portal** — for system-wide oversight and configuration.
2. **Merchant Portal** — for product, shipment, and invoice management.
3. **Dispatcher Portal** — for real-time shipment-to-driver assignment.
4. **Driver Portal** — for route management and delivery status updates.

## 1.3 Project Objectives

The primary objectives of the FastDrop project are:

1. To design and implement a multi-role, web-based delivery management system with strict access control boundaries between stakeholder groups.
2. To develop a multi-warehouse inventory management module that enables merchants to allocate product stock across geographically distributed warehouses while providing administrators with a centralized inventory dashboard.
3. To implement a fleet management module that enforces the business rule requiring drivers to be linked to registered vehicles before becoming eligible for dispatch.
4. To build a dynamic pricing engine that calculates delivery costs (COD amounts and shipping fees) based on configurable geographical zone definitions.
5. To automate the financial reconciliation process through programmatic invoice generation for merchants and structured payment tracking for drivers.
6. To develop a quantitative driver performance scoring system that evaluates delivery success rates, timeliness, and historical reliability.
7. To deliver a modern, responsive, and visually polished user interface that enhances operational efficiency across all user roles.

---

# 2. System Architecture & Technologies

## 2.1 Architectural Pattern

FastDrop follows the **Model-View-Controller (MVC)** architectural pattern, which separates the application into three interconnected layers:

| Layer | Responsibility | Implementation |
|-------|---------------|----------------|
| **Model** | Represents the application's data structures and business entities. | C# POCO classes mapped to SQL Server tables via Entity Framework Core. |
| **View** | Handles the presentation layer and user interface rendering. | Razor Views (.cshtml) with custom CSS and JavaScript. |
| **Controller** | Processes incoming HTTP requests, invokes business logic, and returns appropriate responses. | ASP.NET Core Controller classes organized by functional domain. |

In addition to the core MVC layers, the application employs a **Service Layer** pattern. Business logic is encapsulated within dedicated service classes (e.g., `ShipmentService`, `DispatcherService`, `InvoiceService`) that are injected into controllers via ASP.NET Core's built-in Dependency Injection (DI) container. This promotes separation of concerns, testability, and code reuse.

## 2.2 Technology Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Runtime Framework** | .NET (ASP.NET Core MVC) | .NET 8 |
| **Programming Language** | C# | 12.0 |
| **ORM / Data Access** | Entity Framework Core (Code-First) | 8.x |
| **Database Engine** | Microsoft SQL Server | LocalDB / SQL Server 2019+ |
| **Frontend Markup** | HTML5, Razor Syntax | — |
| **Frontend Styling** | Custom CSS (Variables, Glassmorphism, Responsive Grid) | CSS3 |
| **Client-Side Logic** | Vanilla JavaScript | ES6+ |
| **Authentication** | Cookie-Based Authentication with Role Claims | ASP.NET Core Identity Pipeline |
| **Version Control** | Git / GitHub | — |
| **IDE** | Visual Studio / VS Code | 2022+ |

## 2.3 Project Structure

```
FastDrop.MVC/
├── Controllers/          # 17 controllers handling HTTP requests
│   ├── AccountController.cs
│   ├── AdminPortalController.cs
│   ├── DispatcherPortalController.cs
│   ├── DriverPortalController.cs
│   ├── MerchantPortalController.cs
│   ├── DriversController.cs
│   ├── VehiclesController.cs
│   ├── ShipmentsController.cs
│   ├── ProductsController.cs
│   ├── MerchantsController.cs
│   ├── WarehousesController.cs
│   ├── InvoicesController.cs
│   ├── DriverPaymentsController.cs
│   ├── PerformanceController.cs
│   ├── RoutesController.cs
│   └── ZonesController.cs
├── Models/               # 25 entity classes (Code-First)
├── Services/             # 28 files (14 interfaces + 14 implementations)
├── Views/                # Razor views organized by controller
│   ├── AdminPortal/
│   ├── MerchantPortal/
│   ├── DispatcherPortal/
│   ├── DriverPortal/
│   └── Shared/           # Layout files (_AdminLayout, _MerchantLayout, etc.)
├── Migrations/           # EF Core database migrations
├── ViewModels/           # Composite view models (e.g., AddShipmentViewModel)
└── Program.cs            # Application entry point and DI configuration
```

## 2.4 Authentication & Authorization Architecture

The system implements **cookie-based authentication** with **role-based access control (RBAC)**:

1. **Login Flow:** Users authenticate through the `AccountController`, which validates credentials against the database and issues an authentication cookie containing role claims (e.g., `SuperAdmin`, `Merchant`, `Dispatcher`, `Driver`).
2. **Authorization Enforcement:** Controllers are decorated with `[Authorize(Roles = "...")]` attributes, restricting access to specific user roles. Unauthorized access attempts are automatically redirected to the login page.
3. **Data Isolation:** Beyond role-based page access, controllers enforce data-level isolation. For example, merchants can only view and manage their own products, shipments, and invoices—enforced by filtering queries against the authenticated user's `MerchantId` claim.

---

# 3. Functional Requirements

## 3.1 SuperAdmin Portal

The SuperAdmin role possesses unrestricted system access and is responsible for platform-wide configuration and oversight.

| ID | Requirement | Description |
|----|-------------|-------------|
| FR-A01 | Dashboard Overview | View aggregate metrics: total employees, unpaid invoices, pending driver payments, and registered merchant count. |
| FR-A02 | Employee & Role Management | View all system employees; edit employee profiles including role assignment (SuperAdmin, Dispatcher, etc.) and account status. |
| FR-A03 | Merchant Management | Full CRUD operations on merchant accounts: create, view, edit, and delete merchant profiles including shop name, contact details, and account status. |
| FR-A04 | Warehouse Management | Full CRUD operations on warehouse entities; view warehouse details including assigned product inventory. |
| FR-A05 | Global Inventory Dashboard | View a consolidated inventory table showing all products across all warehouses, including the owning merchant, warehouse location, stock quantity, and stock level status (High/Medium/Low/Empty). |
| FR-A06 | Zone Management | Full CRUD operations on geographical delivery zones including name, city, delivery fee, COD amount, GeoJSON boundary data, and active status. |
| FR-A07 | Vehicle Management | Full CRUD operations on the vehicle fleet: register vehicles with license plate, model, type, maximum capacity, and license expiry date. |
| FR-A08 | Driver Management | Full CRUD operations on driver profiles; assign drivers to vehicles, zones, and managing employees. |
| FR-A09 | Invoice Generation | Generate merchant invoices for a specified date range; view all invoices with payment status. |
| FR-A10 | Driver Payment Management | View all driver payments ordered by date; mark individual payments as "Paid." |
| FR-A11 | Dispatcher Access | Navigate to the Dispatcher Portal for operational shipment assignment. |

## 3.2 Merchant Portal

The Merchant role manages their storefront operations including products, shipments, and financial records.

| ID | Requirement | Description |
|----|-------------|-------------|
| FR-M01 | Dashboard Overview | View aggregate metrics: total shipments created, total COD amount generated, and count of pending (unpaid) invoices. |
| FR-M02 | Product Management | Create and manage products with name, description, price, and multi-warehouse stock allocation. Specify stock quantities independently for each warehouse. |
| FR-M03 | Shipment Creation | Create new shipments by selecting products (with quantities), specifying a delivery address with zone selection, and automatic COD calculation based on zone pricing plus product totals. |
| FR-M04 | Shipment Management | View all shipments with status tracking; edit shipment details (address, weight); delete shipments. View individual shipment details including delivery address and status history. |
| FR-M05 | Invoice Viewing | View all invoices issued to the merchant; access detailed invoice breakdowns showing associated shipments and amounts due. |

## 3.3 Dispatcher Portal

The Dispatcher role serves as the operational bridge between pending shipments and available drivers.

| ID | Requirement | Description |
|----|-------------|-------------|
| FR-D01 | Pending Shipments Queue | View all shipments awaiting assignment (status: "Pending"). |
| FR-D02 | Available Drivers List | View all drivers who are currently eligible for assignment. A driver is considered "available" only if: (a) their status is "available," and (b) they are linked to at least one registered vehicle. |
| FR-D03 | Shipment Assignment | Assign a selected pending shipment to an available driver, triggering status updates and route creation. |

## 3.4 Driver Portal

The Driver role manages their delivery operations and tracks their professional metrics.

| ID | Requirement | Description |
|----|-------------|-------------|
| FR-DR01 | Dashboard Overview | View personal metrics: total assigned routes, completed deliveries, and pending payments. |
| FR-DR02 | Route Management | View all assigned delivery routes with shipment details, addresses, and current status. |
| FR-DR03 | Status Updates | Update shipment delivery status (e.g., "InTransit," "Delivered," "Failed") to reflect real-time progress. |
| FR-DR04 | Payment History | View a chronological record of all payments with amounts and payment status. |
| FR-DR05 | Performance Scores | View personal performance score history including daily scores, tier classification, and weighted historical averages. |

---

# 4. Non-Functional Requirements

## 4.1 Security

| ID | Requirement | Implementation |
|----|-------------|----------------|
| NFR-S01 | Authentication | Cookie-based authentication with secure, HTTP-only cookies. |
| NFR-S02 | Authorization | Role-based access control via `[Authorize]` attributes on all portal controllers. |
| NFR-S03 | Data Isolation | Controller-level enforcement ensuring merchants, drivers, and dispatchers access only their own data. |
| NFR-S04 | CSRF Protection | Anti-forgery tokens (`[ValidateAntiForgeryToken]`) on all POST form submissions. |
| NFR-S05 | Input Validation | Server-side model validation via Data Annotations and `ModelState.IsValid` checks. |

## 4.2 Performance & Reliability

| ID | Requirement | Implementation |
|----|-------------|----------------|
| NFR-P01 | Efficient Data Access | Entity Framework Core with eager loading (`.Include()` / `.ThenInclude()`) to minimize N+1 query problems. |
| NFR-P02 | Optimized Queries | LINQ-based server-side filtering and ordering to reduce data transfer. |
| NFR-P03 | Stateless Controllers | Service-layer pattern with dependency injection for maintainability and horizontal scalability. |

## 4.3 Usability & User Interface

| ID | Requirement | Implementation |
|----|-------------|----------------|
| NFR-U01 | Modern Design Language | Custom CSS design system with CSS custom properties (variables), consistent color palette, and glassmorphism card effects. |
| NFR-U02 | Responsive Layout | Fixed sidebar navigation with fluid content area; media queries for mobile adaptation. |
| NFR-U03 | Role-Specific Layouts | Dedicated layout files (`_AdminLayout`, `_MerchantLayout`, `_DispatcherLayout`, `_DriverLayout`) with tailored navigation, branding, and color accents per role. |
| NFR-U04 | Visual Feedback | Success/error toast notifications via `TempData`; status badges with color-coded indicators (green for active/delivered, red for failed/suspended, amber for pending). |
| NFR-U05 | Typography | Google Fonts integration (Plus Jakarta Sans) for modern, professional typography across all portals. |

---

# 5. Database Design

## 5.1 Overview

The database follows a **Code-First** approach using Entity Framework Core. The schema is defined through C# POCO (Plain Old CLR Object) classes, and the database structure is managed through EF Core migrations. The database name is `dms_db` and is hosted on Microsoft SQL Server.

## 5.2 Entity-Relationship Summary

The system comprises **25 entity classes** organized into the following functional domains:

### 5.2.1 Identity & Access Management

#### Employee

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| EmployeeId | int | PK, Auto-increment | Unique identifier |
| Name | string | Required | Full name |
| Email | string | Required | Login credential |
| Password | string | Required | Hashed password |
| AccountStatus | string | Default: "active" | Account state (active/suspended) |
| RoleId | int? | FK → Role | Assigned system role |
| SupervisorId | int? | FK → Employee (self) | Hierarchical reporting |

#### Role

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| RoleId | int | PK, Auto-increment | Unique identifier |
| RoleName | string | Required | Role name (SuperAdmin, Dispatcher, etc.) |
| PermissionsDescription | string? | Nullable | Human-readable permission summary |

### 5.2.2 Merchant & Product Management

#### Merchant

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| MerchantId | int | PK, Auto-increment | Unique identifier |
| ShopName | string | Required | Business/store name |
| Email | string | Required | Login credential |
| Password | string | Required | Hashed password |
| ContactName | string? | Nullable | Primary contact person |
| Phone | string? | Nullable | Contact phone number |
| Status | string | Default: "active" | Account state |
| AddressId | int? | FK → Address | Business address |

#### Product

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ProductId | int | PK, Auto-increment | Unique identifier |
| Name | string | Required | Product name |
| Description | string? | Nullable | Product description |
| Price | decimal | Required | Unit price |
| StockQuantity | int | Default: 0 | Aggregated total stock across all warehouses |
| MerchantId | int? | FK → Merchant | Owning merchant |

#### ProductWarehouse (Junction Table)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ProductWarehouseId | int | PK, Auto-increment | Unique identifier |
| ProductId | int | FK → Product | Associated product |
| WarehouseId | int | FK → Warehouse | Associated warehouse |
| Quantity | int | Default: 0 | Stock quantity at this specific warehouse |

### 5.2.3 Fleet & Driver Management

#### Vehicle

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| VehicleId | int | PK, Auto-increment | Unique identifier |
| LicensePlate | string | Required | Vehicle registration plate |
| Model | string? | Nullable | Vehicle make/model |
| VehicleType | string? | Nullable | Category (Van, Truck, Motorcycle) |
| MaxCapacityKg | decimal? | Nullable | Maximum load capacity |
| LicenseExpiryDate | DateTime? | Nullable | Registration expiry |

#### Driver

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| DriverId | int | PK, Auto-increment | Unique identifier |
| Name | string | Required | Full name |
| PhoneNumber | string? | Nullable | Contact number |
| LicenseNumber | string? | Nullable | Driving license ID |
| LicenseExpiryDate | DateTime? | Nullable | License expiry |
| CurrentStatus | string | Default: "available" | Operational state |
| ZoneId | int? | FK → Zone | Assigned delivery zone |
| AddressId | int? | FK → Address | Home address |
| EmployeeId | int? | FK → Employee | Managing supervisor |

#### DriverVehicle (Junction Table)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| DriverVehicleId | int | PK, Auto-increment | Unique identifier |
| DriverId | int | FK → Driver | Associated driver |
| VehicleId | int | FK → Vehicle | Associated vehicle |

### 5.2.4 Geography & Routing

#### Zone

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ZoneId | int | PK, Auto-increment | Unique identifier |
| Name | string | Required | Zone name |
| City | string? | Nullable | City name |
| GeojsonBoundary | string? | Nullable | GeoJSON polygon data |
| DeliveryFee | decimal? | Default: 0.00 | Base delivery fee |
| CodAmount | decimal | Default: 0.00 | Cash-on-delivery surcharge |
| ActiveStatus | bool | Default: true | Zone availability |

#### Address

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AddressId | int | PK, Auto-increment | Unique identifier |
| District | string? | Nullable | District/neighborhood |
| Street | string? | Nullable | Street address |
| Latitude | decimal? | Nullable | GPS latitude coordinate |
| Longitude | decimal? | Nullable | GPS longitude coordinate |
| ZoneId | int? | FK → Zone | Geographical zone |

#### Route

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| RouteId | int | PK, Auto-increment | Unique identifier |
| RouteName | string? | Nullable | Descriptive route name |
| Status | string | Default: "active" | Route state |

#### RouteStops

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| RouteStopId | int | PK, Auto-increment | Unique identifier |
| RouteId | int | FK → Route | Parent route |
| AddressId | int? | FK → Address | Stop address |
| StopOrder | int? | Nullable | Sequence number |
| StopType | string? | Nullable | Type (Pickup/Delivery) |

### 5.2.5 Shipment Lifecycle

#### Shipment

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ShipmentId | int | PK, Auto-increment | Unique identifier |
| CreationDate | DateTime | Default: UTC Now | Timestamp of creation |
| CodAmount | decimal | Default: 0.00 | Calculated cash-on-delivery total |
| WeightKg | decimal? | Nullable | Package weight |
| WarehouseId | int? | FK → Warehouse | Source warehouse |
| MerchantId | int? | FK → Merchant | Originating merchant |
| RouteId | int? | FK → Route | Assigned delivery route |
| AddressId | int? | FK → Address | Delivery destination |

#### ShipmentStatus (Audit Trail)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| StatusId | int | PK, Auto-increment | Unique identifier |
| ShipmentId | int | FK → Shipment | Parent shipment |
| Status | string | Required | Status value (Pending, Assigned, InTransit, Delivered, Failed) |
| UpdatedAt | DateTime | Default: UTC Now | Timestamp of status change |

#### ShipmentProduct (Junction Table)

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ShipmentProductId | int | PK, Auto-increment | Unique identifier |
| ShipmentId | int | FK → Shipment | Parent shipment |
| ProductId | int | FK → Product | Included product |
| Quantity | int | Required | Units included |

#### ShipmentAttempt

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AttemptId | int | PK, Auto-increment | Unique identifier |
| ShipmentId | int | FK → Shipment | Parent shipment |
| DriverId | int? | FK → Driver | Attempting driver |
| AttemptDate | DateTime | Default: UTC Now | Attempt timestamp |
| Outcome | string? | Nullable | Result (Success/Failed) |
| FailureReason | string? | Nullable | Reason if failed |

### 5.2.6 Financial Management

#### Invoice

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| InvoiceId | int | PK, Auto-increment | Unique identifier |
| ShipmentId | int? | FK → Shipment | Associated shipment |
| MerchantId | int? | FK → Merchant | Billed merchant |
| AmountDue | decimal | Required | Total amount owed |
| PaymentStatus | string | Default: "pending" | Payment state (pending/paid) |
| PeriodStart | DateTime? | Nullable | Billing period start |
| PeriodEnd | DateTime? | Nullable | Billing period end |

#### DriverPayment

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| PaymentId | int | PK, Auto-increment | Unique identifier |
| DriverId | int? | FK → Driver | Payee driver |
| ShipmentId | int? | FK → Shipment | Associated shipment |
| TotalPayout | decimal? | Default: 0.00 | Payment amount |
| DistanceKm | decimal? | Default: 0.00 | Route distance |
| PaymentStatus | string | Default: "Pending" | Payment state |
| CreatedAt | DateTime | Default: UTC Now | Record timestamp |

### 5.2.7 Performance & Feedback

#### PerformanceScore

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| PerfId | int | PK, Auto-increment | Unique identifier |
| DriverId | int? | FK → Driver | Evaluated driver |
| DailyScore | decimal? | Nullable | Calculated daily performance score |
| Tier | string? | Nullable | Performance tier (Gold/Silver/Bronze) |
| WeightedHistory | decimal? | Nullable | Exponentially weighted historical average |
| RecordedAt | DateTime | Default: UTC Now | Evaluation timestamp |

#### Feedback

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| FeedbackId | int | PK, Auto-increment | Unique identifier |
| ShipmentId | int? | FK → Shipment | Related shipment |
| DriverId | int? | FK → Driver | Related driver |
| Rating | int? | Nullable | Numerical rating |
| Comment | string? | Nullable | Free-text feedback |
| SubmittedAt | DateTime | Default: UTC Now | Submission timestamp |

## 5.3 Entity Relationship Diagram

```
┌──────────┐     ┌──────────┐     ┌──────────────┐
│   Role   │────<│ Employee │>────│   Employee   │
│          │ 1:N │          │ N:1 │ (Supervisor)  │
└──────────┘     └────┬─────┘     └──────────────┘
                      │ 1:N
                 ┌────┴─────┐
                 │  Driver   │>───── Zone
                 └──┬──┬──┬─┘       Address
                    │  │  │
        ┌───────────┘  │  └──────────────────┐
        │ N:M          │ 1:N                 │ 1:N
  ┌─────┴─────┐   ┌────┴────────┐    ┌──────┴────────┐
  │  Vehicle  │   │DriverPayment│    │ Performance   │
  │           │   │             │    │   Score       │
  └───────────┘   └─────────────┘    └───────────────┘

  ┌──────────┐    ┌──────────┐    ┌──────────────────┐
  │ Merchant │───<│ Product  │───<│ ProductWarehouse │>──── Warehouse
  │          │1:N │          │1:N │                  │ N:1
  └────┬─────┘    └────┬─────┘    └──────────────────┘
       │ 1:N           │ N:M
  ┌────┴─────┐    ┌────┴──────────┐
  │ Shipment │───<│ShipmentProduct│
  │          │1:N │               │
  └──┬──┬──┬─┘    └───────────────┘
     │  │  │
     │  │  └──── ShipmentStatus (1:N audit trail)
     │  └─────── ShipmentAttempt (1:N delivery attempts)
     └────────── Invoice (1:N billing records)
```

---

# 6. Key Business Logic

## 6.1 COD Amount Calculation

When a merchant creates a shipment, the Cash-on-Delivery (COD) amount is calculated automatically:

```
COD Amount = Zone COD Base Fee + Σ (Product Price × Quantity)
```

This ensures that the delivery cost dynamically reflects both the geographical delivery zone and the value of the products being shipped.

## 6.2 Driver Eligibility for Dispatch

The Dispatcher Portal enforces a critical business rule: **a driver must be linked to at least one registered vehicle to appear as "available" for shipment assignment.** This is implemented in the `DispatcherService.GetAvailableDriversAsync()` method, which filters drivers based on:

1. `CurrentStatus == "available"`
2. `DriverVehicles.Any() == true` (at least one vehicle assignment exists)

## 6.3 Multi-Warehouse Stock Aggregation

Product stock is tracked at the warehouse level through the `ProductWarehouse` junction table. The total stock displayed on a product is the sum of quantities across all associated warehouses:

```
Product.StockQuantity = Σ (ProductWarehouse.Quantity) for all warehouses
```

## 6.4 Invoice Generation

Invoices are generated by the admin for a merchant over a specified date range. The system queries all delivered shipments within the period and calculates the total amount due based on COD amounts collected.

## 6.5 Performance Scoring

Driver performance is evaluated using a composite scoring model that considers:

- **Daily Score:** Based on successful delivery count and attempt outcomes for a given period.
- **Tier Classification:** Drivers are classified into performance tiers (Gold, Silver, Bronze) based on score thresholds.
- **Weighted History:** An exponentially weighted moving average that smooths performance over time, preventing single bad days from disproportionately affecting the overall score.

---

# 7. Future Enhancements

The following features represent planned extensions to the FastDrop platform:

| Priority | Enhancement | Description |
|----------|-------------|-------------|
| High | **Mobile Application** | Native iOS/Android driver application for real-time delivery updates, photo proof-of-delivery capture, and push notifications. |
| High | **Live GPS Tracking** | Integration with device GPS APIs to provide real-time driver location tracking on an interactive map for dispatchers and merchants. |
| Medium | **AI-Powered Route Optimization** | Machine learning-based route planning that minimizes delivery time and fuel consumption based on historical traffic patterns and delivery density. |
| Medium | **Customer Self-Service Portal** | A public-facing tracking page where end customers can enter a shipment ID to view real-time delivery status without requiring authentication. |
| Medium | **Automated Notifications** | Email and SMS notifications for key events: shipment dispatched, out for delivery, delivered, and failed delivery attempt. |
| Low | **Advanced Analytics Dashboard** | Business intelligence dashboards with charts and graphs covering delivery volumes, revenue trends, zone performance, and driver utilization metrics. |
| Low | **Multi-Language Support** | Internationalization (i18n) framework to support Arabic and English language interfaces. |
| Low | **API Layer (REST)** | A RESTful API layer exposing core functionality for third-party integrations and mobile application backends. |

---

# 8. Conclusion

FastDrop represents a comprehensive solution to the operational challenges faced by modern delivery and logistics organizations. By consolidating merchant management, multi-warehouse inventory tracking, fleet operations, shipment lifecycle management, financial reconciliation, and driver performance evaluation into a single, role-aware web application, the system eliminates the fragmentation and manual processes that characterize traditional delivery workflows.

The system's architecture—built on the proven ASP.NET Core MVC framework with Entity Framework Core and SQL Server—provides a solid, maintainable, and extensible foundation. The service-layer pattern and dependency injection architecture ensure that business logic remains testable and decoupled from presentation concerns. The Code-First database approach, managed through EF Core migrations, enables rapid schema evolution as business requirements change.

The Role-Based Access Control architecture ensures strict data isolation between stakeholder groups, while the modern, responsive user interface—with dedicated layout designs per portal—delivers an intuitive and visually polished experience for all user types.

Looking forward, the platform's modular architecture positions it well for the future enhancements outlined in this document, particularly mobile application integration, real-time GPS tracking, and AI-powered route optimization—features that would further differentiate FastDrop as a competitive logistics management solution.

---

*Document prepared for the Digital Egypt Pioneers Initiative (DEPI) Graduation Project Defense.*
