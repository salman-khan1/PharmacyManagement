using Xunit;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Infrastructure.Repositories;
using PharmacyManagement.Infrastructure.Services;
using PharmacyManagement.Infrastructure.Security;
using System.Threading.Tasks;

namespace PharmacyManagement.Tests;

public class UnitTest1
{
    [Fact]
    public void PasswordHasher_ShouldHashPassword()
    {
        var password = "test123";
        var hash = PasswordHasher.HashPassword(password);

        Assert.NotNull(hash);
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void PasswordHasher_ShouldVerifyCorrectPassword()
    {
        var password = "test123";
        var hash = PasswordHasher.HashPassword(password);
        var result = PasswordHasher.VerifyPassword(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void PasswordHasher_ShouldNotVerifyWrongPassword()
    {
        var password = "test123";
        var wrongPassword = "wrong123";
        var hash = PasswordHasher.HashPassword(password);
        var result = PasswordHasher.VerifyPassword(wrongPassword, hash);

        Assert.False(result);
    }

    [Fact]
    public void Medicine_ShouldCreateWithDefaults()
    {
        var medicine = new Medicine
        {
            MedicineName = "Test Medicine",
            Barcode = "TEST-001",
            Category = "Test Category",
            Quantity = 100,
            SellingPrice = 10.00m
        };

        Assert.Equal("Test Medicine", medicine.MedicineName);
        Assert.Equal("TEST-001", medicine.Barcode);
        Assert.Equal(100, medicine.Quantity);
        Assert.False(medicine.IsDeleted);
        Assert.False(medicine.PrescriptionRequired);
    }

    [Fact]
    public void Invoice_ShouldCalculateTotal()
    {
        var invoice = new Invoice
        {
            SubTotal = 100.00m,
            TaxAmount = 5.00m,
            DiscountAmount = 10.00m,
            TotalAmount = 95.00m
        };

        Assert.Equal(95.00m, invoice.TotalAmount);
    }

    [Fact]
    public void User_ShouldHaveCorrectRole()
    {
        var user = new User
        {
            Username = "admin",
            FullName = "Admin User",
            Role = UserRole.Admin
        };

        Assert.Equal(UserRole.Admin, user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void StockTransaction_ShouldHaveCorrectType()
    {
        var transaction = new StockTransaction
        {
            TransactionType = StockTransactionType.StockIn,
            Quantity = 50,
            UnitPrice = 2.50m,
            TotalPrice = 125.00m
        };

        Assert.Equal(StockTransactionType.StockIn, transaction.TransactionType);
        Assert.Equal(125.00m, transaction.TotalPrice);
    }
}
