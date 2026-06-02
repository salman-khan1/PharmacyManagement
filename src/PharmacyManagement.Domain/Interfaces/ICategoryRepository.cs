using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Domain.Interfaces;

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByNameAsync(string name);
    Task<IEnumerable<Category>> GetActiveAsync();
}
