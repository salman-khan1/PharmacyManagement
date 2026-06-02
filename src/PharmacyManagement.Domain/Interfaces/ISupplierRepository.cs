using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Domain.Interfaces;

public interface ISupplierRepository : IRepository<Supplier>
{
    Task<IEnumerable<Supplier>> GetActiveAsync();
    Task<Supplier?> GetByNameAsync(string name);
}
