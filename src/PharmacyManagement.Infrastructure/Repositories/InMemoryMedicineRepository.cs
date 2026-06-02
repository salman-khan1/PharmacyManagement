using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemoryMedicineRepository : InMemoryRepository<Medicine>, IMedicineRepository
{
    public async Task<Medicine?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        var items = await GetAllAsync();
        return items.FirstOrDefault(m => m.Barcode == barcode && !m.IsDeleted);
    }

    public async Task<IEnumerable<Medicine>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();

        var lower = searchTerm.ToLower();
        var items = await GetAllAsync();
        return items.Where(m =>
            !m.IsDeleted &&
            (m.MedicineName.ToLower().Contains(lower) ||
             m.GenericName.ToLower().Contains(lower) ||
             m.BrandName.ToLower().Contains(lower) ||
             m.Barcode.Contains(lower) ||
             m.Category.ToLower().Contains(lower)));
    }

    public async Task<IEnumerable<Medicine>> GetByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return await GetAllAsync();
        var items = await GetAllAsync();
        return items.Where(m => m.Category == category && !m.IsDeleted);
    }

    public async Task<IEnumerable<Medicine>> GetLowStockAsync()
    {
        var items = await GetAllAsync();
        return items.Where(m => !m.IsDeleted && m.Quantity <= m.MinimumQuantity);
    }

    public async Task<IEnumerable<Medicine>> GetExpiringSoonAsync(int daysThreshold = 30)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
        var items = await GetAllAsync();
        return items.Where(m =>
            !m.IsDeleted &&
            m.ExpiryDate.HasValue &&
            m.ExpiryDate.Value <= thresholdDate &&
            m.ExpiryDate.Value > DateTime.UtcNow);
    }

    public async Task<IEnumerable<Medicine>> GetExpiredAsync()
    {
        var items = await GetAllAsync();
        return items.Where(m =>
            !m.IsDeleted &&
            m.ExpiryDate.HasValue &&
            m.ExpiryDate.Value < DateTime.UtcNow);
    }

    public async Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        var items = await GetAllAsync();
        return items.Where(m => !m.IsDeleted && !string.IsNullOrEmpty(m.Category))
            .Select(m => m.Category)
            .Distinct()
            .ToList();
    }
}
