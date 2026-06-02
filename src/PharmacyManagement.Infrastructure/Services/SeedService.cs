using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Security;

namespace PharmacyManagement.Infrastructure.Services;

public interface ISeedService
{
    Task SeedAsync();
}

public class SeedService : ISeedService
{
    private readonly IUnitOfWork _unitOfWork;

    public SeedService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if already seeded
            var existingUsers = await _unitOfWork.Users.GetAllAsync();
            if (existingUsers.Any()) return;

            await SeedUsersAsync();
            await SeedCategoriesAsync();
            await SeedSuppliersAsync();
            await SeedMedicinesAsync();
            await _unitOfWork.SaveChangesAsync();
        }
        catch
        {
            // Silently handle seed errors - app should still work
        }
    }

    private async Task SeedUsersAsync()
    {
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = PasswordHasher.HashPassword("admin123"),
            FullName = "System Administrator",
            Email = "admin@pharmacy.com",
            Phone = "555-0100",
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var pharmacist = new User
        {
            Username = "pharmacist",
            PasswordHash = PasswordHasher.HashPassword("pharma123"),
            FullName = "John Smith",
            Email = "pharmacist@pharmacy.com",
            Phone = "555-0101",
            Role = UserRole.Pharmacist,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var cashier = new User
        {
            Username = "cashier",
            PasswordHash = PasswordHasher.HashPassword("cash123"),
            FullName = "Sarah Johnson",
            Email = "cashier@pharmacy.com",
            Phone = "555-0102",
            Role = UserRole.Cashier,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Users.AddAsync(adminUser);
        await _unitOfWork.Users.AddAsync(pharmacist);
        await _unitOfWork.Users.AddAsync(cashier);
    }

    private async Task SeedCategoriesAsync()
    {
        var categories = new[]
        {
            "Antibiotics", "Pain Relief", "Vitamins", "Cardiovascular",
            "Diabetes", "Respiratory", "Gastrointestinal", "Dermatology",
            "Antihistamines", "Vaccines"
        };

        foreach (var cat in categories)
        {
            await _unitOfWork.Categories.AddAsync(new Category
            {
                Name = cat,
                Description = $"{cat} medications",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SeedSuppliersAsync()
    {
        var suppliers = new[]
        {
            ("MedSupply Co.", "John Doe", "555-1001", "john@medsupply.com", "123 Main St"),
            ("PharmaCorp", "Jane Smith", "555-1002", "jane@pharmacorp.com", "456 Oak Ave"),
            ("HealthPlus Distributors", "Mike Johnson", "555-1003", "mike@healthplus.com", "789 Elm St"),
            ("Global Pharma", "Lisa Wilson", "555-1004", "lisa@globalpharma.com", "321 Pine Rd"),
            ("CareMed Supplies", "David Brown", "555-1005", "david@caremed.com", "654 Maple Dr")
        };

        foreach (var (name, contact, phone, email, address) in suppliers)
        {
            await _unitOfWork.Suppliers.AddAsync(new Supplier
            {
                Name = name,
                ContactPerson = contact,
                Phone = phone,
                Email = email,
                Address = address,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task SeedMedicinesAsync()
    {
        var medicines = new[]
        {
            ("AMOX-001", "Amoxicillin 500mg", "Amoxicillin", "Moxatag", "Antibiotics", "B2024001", 2.50m, 4.99m, 500, 50, "A1", "S1", "Rack A, Shelf 1", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddMonths(-2), "MedSupply Co.", false, "Broad-spectrum antibiotic"),
            ("IBU-001", "Ibuprofen 400mg", "Ibuprofen", "Advil", "Pain Relief", "B2024002", 1.20m, 2.49m, 800, 100, "A2", "S1", "Rack A, Shelf 1", DateTime.UtcNow.AddYears(1), DateTime.UtcNow.AddMonths(-1), "PharmaCorp", false, "Non-steroidal anti-inflammatory"),
            ("VITC-001", "Vitamin C 1000mg", "Ascorbic Acid", "Nature Made", "Vitamins", "B2024003", 3.00m, 5.99m, 300, 30, "B1", "S2", "Rack B, Shelf 2", DateTime.UtcNow.AddYears(3), DateTime.UtcNow.AddMonths(-3), "HealthPlus Distributors", false, "Immune support vitamin"),
            ("MET-001", "Metformin 500mg", "Metformin HCl", "Glucophage", "Diabetes", "B2024004", 1.50m, 3.49m, 400, 40, "C1", "S3", "Rack C, Shelf 3", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddMonths(-1), "Global Pharma", true, "Type 2 diabetes medication"),
            ("LOR-001", "Loratadine 10mg", "Loratadine", "Claritin", "Antihistamines", "B2024005", 2.00m, 4.49m, 250, 25, "A3", "S1", "Rack A, Shelf 1", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddMonths(-4), "CareMed Supplies", false, "Non-drowsy antihistamine"),
            ("ATEN-001", "Atenolol 50mg", "Atenolol", "Tenormin", "Cardiovascular", "B2024006", 1.80m, 3.99m, 350, 35, "C2", "S3", "Rack C, Shelf 3", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddMonths(-2), "MedSupply Co.", true, "Beta-blocker for hypertension"),
            ("OMEP-001", "Omeprazole 20mg", "Omeprazole", "Prilosec", "Gastrointestinal", "B2024007", 2.20m, 4.99m, 600, 60, "D1", "S4", "Rack D, Shelf 4", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddMonths(-1), "PharmaCorp", false, "Proton pump inhibitor"),
            ("SALB-001", "Salbutamol Inhaler", "Albuterol", "Ventolin", "Respiratory", "B2024008", 4.00m, 8.99m, 150, 15, "D2", "S4", "Rack D, Shelf 4", DateTime.UtcNow.AddYears(1), DateTime.UtcNow.AddMonths(-5), "HealthPlus Distributors", true, "Bronchodilator inhaler"),
            ("CET-001", "Cetirizine 10mg", "Cetirizine HCl", "Zyrtec", "Antihistamines", "B2024009", 1.75m, 3.99m, 450, 45, "A3", "S1", "Rack A, Shelf 1", DateTime.UtcNow.AddYears(2), DateTime.UtcNow.AddMonths(-3), "Global Pharma", false, "Antihistamine for allergies"),
            ("INS-001", "Insulin Glargine", "Insulin Glargine", "Lantus", "Diabetes", "B2024010", 25.00m, 49.99m, 100, 10, "C1", "S3", "Rack C, Shelf 3", DateTime.UtcNow.AddYears(1), DateTime.UtcNow.AddMonths(-1), "CareMed Supplies", true, "Long-acting insulin analogue")
        };

        foreach (var (barcode, name, generic, brand, category, batch, purchase, selling, qty, minQty, rack, shelf, location, expiry, mfg, supplier, prescription, desc) in medicines)
        {
            await _unitOfWork.Medicines.AddAsync(new Medicine
            {
                Barcode = barcode,
                MedicineName = name,
                GenericName = generic,
                BrandName = brand,
                Category = category,
                BatchNumber = batch,
                PurchasePrice = purchase,
                SellingPrice = selling,
                Quantity = qty,
                MinimumQuantity = minQty,
                RackNumber = rack,
                ShelfNumber = shelf,
                ExactLocation = location,
                ExpiryDate = expiry,
                ManufacturingDate = mfg,
                Supplier = supplier,
                PrescriptionRequired = prescription,
                Description = desc,
                ImagePath = string.Empty,
                CreatedAt = DateTime.UtcNow
            });
        }
    }
}
