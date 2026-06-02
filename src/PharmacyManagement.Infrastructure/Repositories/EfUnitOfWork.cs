using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Persistence.Data;

namespace PharmacyManagement.Infrastructure.Repositories;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly PharmacyDbContext _context;
    private bool _disposed;

    public IMedicineRepository Medicines { get; }
    public IUserRepository Users { get; }
    public IStockTransactionRepository StockTransactions { get; }
    public IInvoiceRepository Invoices { get; }
    public ISupplierRepository Suppliers { get; }
    public ICategoryRepository Categories { get; }

    public EfUnitOfWork(PharmacyDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        Medicines = new EfMedicineRepository(context);
        Users = new EfUserRepository(context);
        StockTransactions = new EfStockTransactionRepository(context);
        Invoices = new EfInvoiceRepository(context);
        Suppliers = new EfSupplierRepository(context);
        Categories = new EfCategoryRepository(context);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            _disposed = true;
        }
    }
}
