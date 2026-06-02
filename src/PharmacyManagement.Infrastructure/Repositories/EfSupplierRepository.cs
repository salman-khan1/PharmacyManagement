using Microsoft.EntityFrameworkCore;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Persistence.Data;

namespace PharmacyManagement.Infrastructure.Repositories;

public class EfSupplierRepository : EfRepository<Supplier>, ISupplierRepository
{
    public EfSupplierRepository(PharmacyDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Supplier>> GetActiveAsync()
    {
        return await _dbSet.Where(s => s.IsActive && !s.IsDeleted).ToListAsync();
    }

    public async Task<Supplier?> GetByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return await _dbSet.FirstOrDefaultAsync(s => s.Name == name && !s.IsDeleted);
    }
}
