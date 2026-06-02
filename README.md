# Pharmacy Management System

A complete, production-ready Pharmacy Management System built with C# .NET 8, WPF, MVVM, Entity Framework Core, and SQLite.

## Features

### Medicine Management
- Add, update, delete medicines
- Search by name, generic, brand, barcode, or category
- Barcode support
- Category filtering
- Prescription-required tracking
- Stock quantity and minimum level alerts
- Expiry date tracking
- Location tracking (rack, shelf, exact location)
- Supplier management

### Inventory Module
- Stock In/Out operations
- Stock adjustment
- Damaged stock handling
- Return to supplier
- Low stock alerts
- Expiry alerts
- Transaction history

### Point of Sale (POS)
- Fast medicine search
- Barcode scanning
- Shopping cart management
- Discount handling
- Tax calculation (configurable rate)
- Multiple payment methods (Cash, Card, Insurance, Mobile)
- Invoice generation
- Refund support

### Reports & Analytics
- Daily sales report
- Monthly sales report
- Inventory report
- Expiry report
- Supplier report
- Profit report with margin calculation
- Export to Excel, CSV, and PDF

### Security
- BCrypt password hashing
- Role-based authorization (Admin, Pharmacist, Cashier, Manager)
- Input validation
- Global exception handling

### System Features
- Dark/Light theme toggle
- Serilog logging with daily rolling files
- Offline-first design (works without database)
- Automatic fallback to in-memory storage
- Seed data for initial setup
- Clean Architecture with SOLID principles

## Architecture

```
PharmacyManagement/
  src/
    PharmacyManagement.Domain/          # Entities, Interfaces, Enums
    PharmacyManagement.Persistence/     # EF Core DbContext, Migrations
    PharmacyManagement.Infrastructure/  # Repositories, Services, Logging, Export
    PharmacyManagement.Application/     # Application layer
    PharmacyManagement.UI/              # WPF Views, ViewModels, Themes
  tests/
    PharmacyManagement.Tests/           # Unit tests
```

## Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11 (for WPF)
- Visual Studio 2022 or JetBrains Rider (recommended)

## Getting Started

### Quick Start

1. Clone or download the solution
2. Open `PharmacyManagement.sln` in Visual Studio
3. Set `PharmacyManagement.UI` as startup project
4. Press F5 to build and run

### Default Login Credentials

| Username     | Password    | Role        |
|------------- |------------ |------------ |
| admin        | admin123    | Admin       |
| pharmacist   | pharma123   | Pharmacist  |
| cashier      | cash123     | Cashier     |

### Configuration

Edit `appsettings.json` in the UI project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=pharmacy.db"
  },
  "Logging": {
    "LogFilePath": "logs/pharmacy-.log"
  }
}
```

- **With database**: Set `DefaultConnection` to a valid SQLite connection string
- **Without database**: Leave `DefaultConnection` empty or remove it - the app will use in-memory storage

## Building from Command Line

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run application
dotnet run --project src/PharmacyManagement.UI
```

## Project Structure

### Domain Layer
- **Models**: Medicine, User, StockTransaction, Invoice, InvoiceItem, Supplier, Category
- **Interfaces**: Repository interfaces, Unit of Work pattern
- **Enums**: UserRole, StockTransactionType, PaymentMethod, InvoiceStatus

### Persistence Layer
- **DbContext**: Entity Framework Core with SQLite support
- **Entity Configurations**: Indexing, query filters, relationships

### Infrastructure Layer
- **Repositories**: EF Core and In-Memory implementations
- **Services**: Auth, Inventory, Sales, Reports, Export, Seed
- **Security**: BCrypt password hashing
- **Logging**: Serilog with file sink

### UI Layer
- **Views**: WPF XAML views with MVVM
- **ViewModels**: CommunityToolkit.Mvvm based ViewModels
- **Themes**: Dark and Light theme support
- **Converters**: Value converters for binding

## Offline-First Design

The application automatically detects whether a database connection is configured:

- **With connection string**: Uses SQLite with EF Core migrations
- **Without connection string**: Falls back to in-memory storage

All features work in both modes. Data persistence in offline mode is limited to the current session.

## Technology Stack

- **.NET 8.0**
- **WPF** (Windows Presentation Foundation)
- **MVVM** with CommunityToolkit.Mvvm
- **Entity Framework Core 8**
- **SQLite**
- **BCrypt.Net**
- **EPPlus** (Excel export)
- **Serilog** (Logging)

## License

This project is provided as-is for educational and commercial use.
