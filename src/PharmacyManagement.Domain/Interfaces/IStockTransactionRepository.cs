using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Domain.Interfaces;

public interface IStockTransactionRepository : IRepository<StockTransaction>
{
    Task<IEnumerable<StockTransaction>> GetByMedicineIdAsync(Guid medicineId);
    Task<IEnumerable<StockTransaction>> GetByTypeAsync(StockTransactionType type);
    Task<IEnumerable<StockTransaction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}
