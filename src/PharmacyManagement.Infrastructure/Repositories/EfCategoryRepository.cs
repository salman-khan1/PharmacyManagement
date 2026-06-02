using Microsoft.EntityFrameworkCore;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Persistence.Data;

namespace PharmacyManagement.Infrastructure.Repositories;

public class EfCategoryRepository : EfRepository<Category>, ICategoryRepository
{
    public EfCategoryRepository(PharmacyDbContext context) : base(context)
    {
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return await _dbSet.FirstOrDefaultAsync(c => c.Name == name && !c.IsDeleted);
    }

    public async Task<IEnumerable<Category>> GetActiveAsync()
    {
        return await _dbSet.Where(c => c.IsActive && !c.IsDeleted).ToListAsync();
    }
}
