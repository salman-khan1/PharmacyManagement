using Microsoft.EntityFrameworkCore;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Persistence.Data;

namespace PharmacyManagement.Infrastructure.Repositories;

public class EfStockTransactionRepository : EfRepository<StockTransaction>, IStockTransactionRepository
{
    public EfStockTransactionRepository(PharmacyDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<StockTransaction>> GetByMedicineIdAsync(Guid medicineId)
    {
        return await _dbSet.Where(st => st.MedicineId == medicineId && !st.IsDeleted)
            .Include(st => st.Medicine)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockTransaction>> GetByTypeAsync(StockTransactionType type)
    {
        return await _dbSet.Where(st => st.TransactionType == type && !st.IsDeleted)
            .Include(st => st.Medicine)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.Where(st => st.CreatedAt >= startDate && st.CreatedAt <= endDate && !st.IsDeleted)
            .Include(st => st.Medicine)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }
}
