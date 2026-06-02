using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Domain.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status);
    Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate);
    Task<int> GetInvoiceCountAsync(DateTime startDate, DateTime endDate);
}
