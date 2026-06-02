using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Domain.Interfaces;

public interface IMedicineRepository : IRepository<Medicine>
{
    Task<Medicine?> GetByBarcodeAsync(string barcode);
    Task<IEnumerable<Medicine>> SearchAsync(string searchTerm);
    Task<IEnumerable<Medicine>> GetByCategoryAsync(string category);
    Task<IEnumerable<Medicine>> GetLowStockAsync();
    Task<IEnumerable<Medicine>> GetExpiringSoonAsync(int daysThreshold = 30);
    Task<IEnumerable<Medicine>> GetExpiredAsync();
    Task<IEnumerable<string>> GetAllCategoriesAsync();
}
