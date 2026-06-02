using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemoryInvoiceRepository : InMemoryRepository<Invoice>, IInvoiceRepository
{
    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return null;
        var items = await GetAllAsync();
        return items.FirstOrDefault(i => i.InvoiceNumber == invoiceNumber && !i.IsDeleted);
    }

    public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        var items = await GetAllAsync();
        return items.Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt);
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
    {
        var items = await GetAllAsync();
        return items.Where(i => i.Status == status && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt);
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate)
    {
        var items = await GetAllAsync();
        return items.Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted && i.Status == InvoiceStatus.Paid)
            .Sum(i => i.TotalAmount);
    }

    public async Task<int> GetInvoiceCountAsync(DateTime startDate, DateTime endDate)
    {
        var items = await GetAllAsync();
        return items.Count(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted);
    }
}
