using PharmacyManagement.Domain.Interfaces;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemoryUnitOfWork : IUnitOfWork
{
    private bool _disposed;

    public IMedicineRepository Medicines { get; }
    public IUserRepository Users { get; }
    public IStockTransactionRepository StockTransactions { get; }
    public IInvoiceRepository Invoices { get; }
    public ISupplierRepository Suppliers { get; }
    public ICategoryRepository Categories { get; }

    public InMemoryUnitOfWork()
    {
        Medicines = new InMemoryMedicineRepository();
        Users = new InMemoryUserRepository();
        StockTransactions = new InMemoryStockTransactionRepository();
        Invoices = new InMemoryInvoiceRepository();
        Suppliers = new InMemorySupplierRepository();
        Categories = new InMemoryCategoryRepository();
    }

    public Task<int> SaveChangesAsync()
    {
        // In-memory operations are synchronous, so return completed task
        return Task.FromResult(1);
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
            _disposed = true;
        }
    }
}
