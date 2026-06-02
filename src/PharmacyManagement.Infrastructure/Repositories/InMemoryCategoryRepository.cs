using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemoryCategoryRepository : InMemoryRepository<Category>, ICategoryRepository
{
    public async Task<Category?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var items = await GetAllAsync();
        return items.FirstOrDefault(c => c.Name == name && !c.IsDeleted);
    }

    public async Task<IEnumerable<Category>> GetActiveAsync()
    {
        var items = await GetAllAsync();
        return items.Where(c => c.IsActive && !c.IsDeleted);
    }
}
