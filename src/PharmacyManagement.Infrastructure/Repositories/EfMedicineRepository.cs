using Microsoft.EntityFrameworkCore;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Persistence.Data;

namespace PharmacyManagement.Infrastructure.Repositories;

public class EfMedicineRepository : EfRepository<Medicine>, IMedicineRepository
{
    public EfMedicineRepository(PharmacyDbContext context) : base(context)
    {
    }

    public async Task<Medicine?> GetByBarcodeAsync(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        return await _dbSet.FirstOrDefaultAsync(m => m.Barcode == barcode && !m.IsDeleted);
    }

    public async Task<IEnumerable<Medicine>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();

        var lower = searchTerm.ToLower();
        return await _dbSet.Where(m =>
            !m.IsDeleted &&
            (m.MedicineName.ToLower().Contains(lower) ||
             m.GenericName.ToLower().Contains(lower) ||
             m.BrandName.ToLower().Contains(lower) ||
             m.Barcode.Contains(lower) ||
             m.Category.ToLower().Contains(lower)))
            .ToListAsync();
    }

    public async Task<IEnumerable<Medicine>> GetByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return await GetAllAsync();
        return await _dbSet.Where(m => m.Category == category && !m.IsDeleted).ToListAsync();
    }

    public async Task<IEnumerable<Medicine>> GetLowStockAsync()
    {
        return await _dbSet.Where(m => !m.IsDeleted && m.Quantity <= m.MinimumQuantity).ToListAsync();
    }

    public async Task<IEnumerable<Medicine>> GetExpiringSoonAsync(int daysThreshold = 30)
    {
        var thresholdDate = DateTime.UtcNow.AddDays(daysThreshold);
        return await _dbSet.Where(m =>
            !m.IsDeleted &&
            m.ExpiryDate.HasValue &&
            m.ExpiryDate.Value <= thresholdDate &&
            m.ExpiryDate.Value > DateTime.UtcNow).ToListAsync();
    }

    public async Task<IEnumerable<Medicine>> GetExpiredAsync()
    {
        return await _dbSet.Where(m =>
            !m.IsDeleted &&
            m.ExpiryDate.HasValue &&
            m.ExpiryDate.Value < DateTime.UtcNow).ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllCategoriesAsync()
    {
        return await _dbSet.Where(m => !m.IsDeleted && !string.IsNullOrEmpty(m.Category))
            .Select(m => m.Category)
            .Distinct()
            .ToListAsync();
    }
}
