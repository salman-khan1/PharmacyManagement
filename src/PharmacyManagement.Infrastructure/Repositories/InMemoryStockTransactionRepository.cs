using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemoryStockTransactionRepository : InMemoryRepository<StockTransaction>, IStockTransactionRepository
{
    public async Task<IEnumerable<StockTransaction>> GetByMedicineIdAsync(Guid medicineId)
    {
        var items = await GetAllAsync();
        return items.Where(st => st.MedicineId == medicineId && !st.IsDeleted)
            .OrderByDescending(st => st.CreatedAt);
    }

    public async Task<IEnumerable<StockTransaction>> GetByTypeAsync(StockTransactionType type)
    {
        var items = await GetAllAsync();
        return items.Where(st => st.TransactionType == type && !st.IsDeleted)
            .OrderByDescending(st => st.CreatedAt);
    }

    public async Task<IEnumerable<StockTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var items = await GetAllAsync();
        return items.Where(st => st.CreatedAt >= startDate && st.CreatedAt <= endDate && !st.IsDeleted)
            .OrderByDescending(st => st.CreatedAt);
    }
}
