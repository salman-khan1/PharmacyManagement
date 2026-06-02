using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Services;

public interface IReportService
{
    Task<SalesReport> GetDailySalesReportAsync(DateTime date);
    Task<SalesReport> GetMonthlySalesReportAsync(int year, int month);
    Task<InventoryReport> GetInventoryReportAsync();
    Task<ExpiryReport> GetExpiryReportAsync(int daysThreshold = 30);
    Task<SupplierReport> GetSupplierReportAsync();
    Task<ProfitReport> GetProfitReportAsync(DateTime startDate, DateTime endDate);
}

public class SalesReport
{
    public DateTime ReportDate { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalDiscount { get; set; }
    public List<PaymentMethodSummary> PaymentSummaries { get; set; } = new();
    public List<TopSellingItem> TopSellingItems { get; set; } = new();
}

public class PaymentMethodSummary
{
    public PaymentMethod Method { get; set; }
    public int Count { get; set; }
    public decimal Total { get; set; }
}

public class TopSellingItem
{
    public string MedicineName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class InventoryReport
{
    public int TotalMedicines { get; set; }
    public int TotalCategories { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int LowStockCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public List<CategorySummary> CategorySummaries { get; set; } = new();
    public List<Medicine> LowStockItems { get; set; } = new();
}

public class CategorySummary
{
    public string Category { get; set; } = string.Empty;
    public int MedicineCount { get; set; }
    public decimal TotalValue { get; set; }
}

public class ExpiryReport
{
    public int ExpiredCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public List<Medicine> ExpiredItems { get; set; } = new();
    public List<Medicine> ExpiringSoonItems { get; set; } = new();
}

public class SupplierReport
{
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
    public List<Supplier> Suppliers { get; set; } = new();
}

public class ProfitReport
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public int TotalTransactions { get; set; }
}

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SalesReport> GetDailySalesReportAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        var invoices = await _unitOfWork.Invoices.GetByDateRangeAsync(start, end);
        var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();

        var report = new SalesReport
        {
            ReportDate = date,
            TotalInvoices = paidInvoices.Count,
            TotalSales = paidInvoices.Sum(i => i.TotalAmount),
            TotalTax = paidInvoices.Sum(i => i.TaxAmount),
            TotalDiscount = paidInvoices.Sum(i => i.DiscountAmount)
        };

        var paymentGroups = paidInvoices.GroupBy(i => i.PaymentMethod)
            .Select(g => new PaymentMethodSummary
            {
                Method = g.Key,
                Count = g.Count(),
                Total = g.Sum(i => i.TotalAmount)
            }).ToList();
        report.PaymentSummaries = paymentGroups;

        // Top selling items
        var itemGroups = paidInvoices.SelectMany(i => i.Items)
            .GroupBy(item => item.MedicineName)
            .Select(g => new TopSellingItem
            {
                MedicineName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(10)
            .ToList();
        report.TopSellingItems = itemGroups;

        return report;
    }

    public async Task<SalesReport> GetMonthlySalesReportAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var dailyReports = new List<SalesReport>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            dailyReports.Add(await GetDailySalesReportAsync(date));
        }

        return new SalesReport
        {
            ReportDate = start,
            TotalInvoices = dailyReports.Sum(r => r.TotalInvoices),
            TotalSales = dailyReports.Sum(r => r.TotalSales),
            TotalTax = dailyReports.Sum(r => r.TotalTax),
            TotalDiscount = dailyReports.Sum(r => r.TotalDiscount),
            PaymentSummaries = dailyReports.SelectMany(r => r.PaymentSummaries)
                .GroupBy(p => p.Method)
                .Select(g => new PaymentMethodSummary
                {
                    Method = g.Key,
                    Count = g.Sum(p => p.Count),
                    Total = g.Sum(p => p.Total)
                }).ToList(),
            TopSellingItems = dailyReports.SelectMany(r => r.TopSellingItems)
                .GroupBy(t => t.MedicineName)
                .Select(g => new TopSellingItem
                {
                    MedicineName = g.Key,
                    QuantitySold = g.Sum(t => t.QuantitySold),
                    TotalRevenue = g.Sum(t => t.TotalRevenue)
                })
                .OrderByDescending(t => t.QuantitySold)
                .Take(10)
                .ToList()
        };
    }

    public async Task<InventoryReport> GetInventoryReportAsync()
    {
        var medicines = await _unitOfWork.Medicines.GetAllAsync();
        var medicineList = medicines.ToList();
        var lowStock = await _unitOfWork.Medicines.GetLowStockAsync();
        var expiringSoon = await _unitOfWork.Medicines.GetExpiringSoonAsync(30);
        var expired = await _unitOfWork.Medicines.GetExpiredAsync();
        var categories = await _unitOfWork.Categories.GetAllAsync();

        var categorySummaries = medicineList
            .GroupBy(m => m.Category)
            .Select(g => new CategorySummary
            {
                Category = g.Key,
                MedicineCount = g.Count(),
                TotalValue = g.Sum(m => m.PurchasePrice * m.Quantity)
            }).ToList();

        return new InventoryReport
        {
            TotalMedicines = medicineList.Count,
            TotalCategories = categories.Count(),
            TotalInventoryValue = medicineList.Sum(m => m.PurchasePrice * m.Quantity),
            LowStockCount = lowStock.Count(),
            ExpiringSoonCount = expiringSoon.Count(),
            ExpiredCount = expired.Count(),
            CategorySummaries = categorySummaries,
            LowStockItems = lowStock.ToList()
        };
    }

    public async Task<ExpiryReport> GetExpiryReportAsync(int daysThreshold = 30)
    {
        var expired = await _unitOfWork.Medicines.GetExpiredAsync();
        var expiringSoon = await _unitOfWork.Medicines.GetExpiringSoonAsync(daysThreshold);

        return new ExpiryReport
        {
            ExpiredCount = expired.Count(),
            ExpiringSoonCount = expiringSoon.Count(),
            ExpiredItems = expired.ToList(),
            ExpiringSoonItems = expiringSoon.ToList()
        };
    }

    public async Task<SupplierReport> GetSupplierReportAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        var active = await _unitOfWork.Suppliers.GetActiveAsync();

        return new SupplierReport
        {
            TotalSuppliers = suppliers.Count(),
            ActiveSuppliers = active.Count(),
            Suppliers = suppliers.ToList()
        };
    }

    public async Task<ProfitReport> GetProfitReportAsync(DateTime startDate, DateTime endDate)
    {
        var invoices = await _unitOfWork.Invoices.GetByDateRangeAsync(startDate, endDate);
        var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();

        var totalRevenue = paidInvoices.Sum(i => i.TotalAmount);

        // Calculate cost from stock transactions
        var transactions = await _unitOfWork.StockTransactions.GetByDateRangeAsync(startDate, endDate);
        var saleTransactions = transactions.Where(t => t.TransactionType == StockTransactionType.Sale).ToList();
        var totalCost = saleTransactions.Sum(t => t.TotalPrice != 0 ? t.Quantity * (t.TotalPrice / t.Quantity) : 0);

        var grossProfit = totalRevenue - totalCost;
        var profitMargin = totalRevenue > 0 ? (grossProfit / totalRevenue) * 100 : 0;

        return new ProfitReport
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalRevenue = totalRevenue,
            TotalCost = totalCost,
            GrossProfit = grossProfit,
            ProfitMargin = profitMargin,
            TotalTransactions = paidInvoices.Count
        };
    }
}
