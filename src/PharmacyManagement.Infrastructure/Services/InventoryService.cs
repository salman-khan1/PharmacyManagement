using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Services;

public interface IInventoryService
{
    Task<StockTransaction> StockInAsync(Guid medicineId, int quantity, decimal unitPrice, string supplier, string reason, string referenceNumber);
    Task<StockTransaction> StockOutAsync(Guid medicineId, int quantity, string reason, string referenceNumber);
    Task<StockTransaction> AdjustStockAsync(Guid medicineId, int newQuantity, string reason);
    Task<StockTransaction> MarkDamagedAsync(Guid medicineId, int quantity, string reason);
    Task<StockTransaction> ReturnStockAsync(Guid medicineId, int quantity, string supplier, string reason);
    Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync();
    Task<IEnumerable<Medicine>> GetExpiringMedicinesAsync(int daysThreshold = 30);
    Task<IEnumerable<Medicine>> GetExpiredMedicinesAsync();
    Task<IEnumerable<StockTransaction>> GetTransactionHistoryAsync(Guid? medicineId = null, DateTime? startDate = null, DateTime? endDate = null);
}

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<StockTransaction> StockInAsync(Guid medicineId, int quantity, decimal unitPrice, string supplier, string reason, string referenceNumber)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
        if (medicine == null) throw new InvalidOperationException("Medicine not found");

        medicine.Quantity += quantity;
        medicine.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Medicines.UpdateAsync(medicine);

        var transaction = new StockTransaction
        {
            MedicineId = medicineId,
            TransactionType = StockTransactionType.StockIn,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            Supplier = supplier ?? string.Empty,
            Reason = reason ?? "Stock In",
            ReferenceNumber = referenceNumber ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StockTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return transaction;
    }

    public async Task<StockTransaction> StockOutAsync(Guid medicineId, int quantity, string reason, string referenceNumber)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
        if (medicine == null) throw new InvalidOperationException("Medicine not found");
        if (medicine.Quantity < quantity) throw new InvalidOperationException("Insufficient stock");

        medicine.Quantity -= quantity;
        medicine.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Medicines.UpdateAsync(medicine);

        var transaction = new StockTransaction
        {
            MedicineId = medicineId,
            TransactionType = StockTransactionType.StockOut,
            Quantity = quantity,
            UnitPrice = medicine.SellingPrice,
            TotalPrice = quantity * medicine.SellingPrice,
            Reason = reason ?? "Stock Out",
            ReferenceNumber = referenceNumber ?? string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StockTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return transaction;
    }

    public async Task<StockTransaction> AdjustStockAsync(Guid medicineId, int newQuantity, string reason)
    {
        if (newQuantity < 0) throw new ArgumentException("Quantity cannot be negative", nameof(newQuantity));

        var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
        if (medicine == null) throw new InvalidOperationException("Medicine not found");

        var difference = newQuantity - medicine.Quantity;

        var transaction = new StockTransaction
        {
            MedicineId = medicineId,
            TransactionType = StockTransactionType.Adjustment,
            Quantity = Math.Abs(difference),
            UnitPrice = medicine.PurchasePrice,
            TotalPrice = Math.Abs(difference) * medicine.PurchasePrice,
            Reason = reason ?? $"Stock adjusted from {medicine.Quantity} to {newQuantity}",
            ReferenceNumber = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        medicine.Quantity = newQuantity;
        medicine.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Medicines.UpdateAsync(medicine);
        await _unitOfWork.StockTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return transaction;
    }

    public async Task<StockTransaction> MarkDamagedAsync(Guid medicineId, int quantity, string reason)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
        if (medicine == null) throw new InvalidOperationException("Medicine not found");
        if (medicine.Quantity < quantity) throw new InvalidOperationException("Insufficient stock");

        medicine.Quantity -= quantity;
        medicine.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Medicines.UpdateAsync(medicine);

        var transaction = new StockTransaction
        {
            MedicineId = medicineId,
            TransactionType = StockTransactionType.Damaged,
            Quantity = quantity,
            UnitPrice = medicine.PurchasePrice,
            TotalPrice = quantity * medicine.PurchasePrice,
            Reason = reason ?? "Damaged stock",
            ReferenceNumber = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StockTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return transaction;
    }

    public async Task<StockTransaction> ReturnStockAsync(Guid medicineId, int quantity, string supplier, string reason)
    {
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
        if (medicine == null) throw new InvalidOperationException("Medicine not found");
        if (medicine.Quantity < quantity) throw new InvalidOperationException("Insufficient stock");

        medicine.Quantity -= quantity;
        medicine.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Medicines.UpdateAsync(medicine);

        var transaction = new StockTransaction
        {
            MedicineId = medicineId,
            TransactionType = StockTransactionType.Returned,
            Quantity = quantity,
            UnitPrice = medicine.PurchasePrice,
            TotalPrice = quantity * medicine.PurchasePrice,
            Supplier = supplier ?? medicine.Supplier,
            Reason = reason ?? "Returned to supplier",
            ReferenceNumber = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.StockTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return transaction;
    }

    public async Task<IEnumerable<Medicine>> GetLowStockMedicinesAsync()
    {
        return await _unitOfWork.Medicines.GetLowStockAsync();
    }

    public async Task<IEnumerable<Medicine>> GetExpiringMedicinesAsync(int daysThreshold = 30)
    {
        return await _unitOfWork.Medicines.GetExpiringSoonAsync(daysThreshold);
    }

    public async Task<IEnumerable<Medicine>> GetExpiredMedicinesAsync()
    {
        return await _unitOfWork.Medicines.GetExpiredAsync();
    }

    public async Task<IEnumerable<StockTransaction>> GetTransactionHistoryAsync(Guid? medicineId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        if (medicineId.HasValue)
        {
            return await _unitOfWork.StockTransactions.GetByMedicineIdAsync(medicineId.Value);
        }

        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        return await _unitOfWork.StockTransactions.GetByDateRangeAsync(start, end);
    }
}
