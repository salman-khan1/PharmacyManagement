using Microsoft.EntityFrameworkCore;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Persistence.Data;

namespace PharmacyManagement.Infrastructure.Repositories;

public class EfInvoiceRepository : EfRepository<Invoice>, IInvoiceRepository
{
    public EfInvoiceRepository(PharmacyDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetByInvoiceNumberAsync(string invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) return null;
        return await _dbSet.Include(i => i.Items)
            .ThenInclude(ii => ii.Medicine)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && !i.IsDeleted);
    }

    public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.Include(i => i.Items)
            .Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Invoice>> GetByStatusAsync(InvoiceStatus status)
    {
        return await _dbSet.Include(i => i.Items)
            .Where(i => i.Status == status && !i.IsDeleted)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.Where(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted && i.Status == InvoiceStatus.Paid)
            .SumAsync(i => i.TotalAmount);
    }

    public async Task<int> GetInvoiceCountAsync(DateTime startDate, DateTime endDate)
    {
        return await _dbSet.CountAsync(i => i.CreatedAt >= startDate && i.CreatedAt <= endDate && !i.IsDeleted);
    }
}
