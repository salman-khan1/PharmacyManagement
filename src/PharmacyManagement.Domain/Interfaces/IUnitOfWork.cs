namespace PharmacyManagement.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IMedicineRepository Medicines { get; }
    IUserRepository Users { get; }
    IStockTransactionRepository StockTransactions { get; }
    IInvoiceRepository Invoices { get; }
    ISupplierRepository Suppliers { get; }
    ICategoryRepository Categories { get; }
    Task<int> SaveChangesAsync();
}
