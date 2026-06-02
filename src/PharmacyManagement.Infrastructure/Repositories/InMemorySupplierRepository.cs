using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemorySupplierRepository : InMemoryRepository<Supplier>, ISupplierRepository
{
    public async Task<IEnumerable<Supplier>> GetActiveAsync()
    {
        var items = await GetAllAsync();
        return items.Where(s => s.IsActive && !s.IsDeleted);
    }

    public async Task<Supplier?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var items = await GetAllAsync();
        return items.FirstOrDefault(s => s.Name == name && !s.IsDeleted);
    }
}
