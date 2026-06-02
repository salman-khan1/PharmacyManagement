using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Export;
using PharmacyManagement.Infrastructure.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace PharmacyManagement.UI.ViewModels;

public partial class ReportsViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportService _reportService;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private DateTime _startDate = DateTime.UtcNow.AddDays(-30);

    [ObservableProperty]
    private DateTime _endDate = DateTime.UtcNow;

    [ObservableProperty]
    private SalesReport? _salesReport;

    private bool _hasSalesReport;

    [ObservableProperty]
    private InventoryReport? _inventoryReport;

    private bool _hasInventoryReport;

    [ObservableProperty]
    private ExpiryReport? _expiryReport;

    private bool _hasExpiryReport;

    private bool _hasProfitReport;

    [ObservableProperty]
    private SupplierReport? _supplierReport;

    [ObservableProperty]
    private ProfitReport? _profitReport;

    [ObservableProperty]
    private ObservableCollection<Invoice> _recentInvoices = new();

    [ObservableProperty]
    private int _selectedReportIndex;

    // Flags for UI visibility
    public bool HasSalesReport
    {
        get => _hasSalesReport;
        set => SetProperty(ref _hasSalesReport, value);
    }

    [RelayCommand]
    private async Task DeleteInvoiceAsync(Invoice invoice)
    {
        try
        {
            if (invoice == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete invoice {invoice.InvoiceNumber}?",
                "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            invoice.IsDeleted = true;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Invoices.UpdateAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            // remove from recent invoices collection
            RecentInvoices?.Remove(invoice);

            // Recalculate SalesReport preview if present
            if (RecentInvoices != null && RecentInvoices.Any())
            {
                SalesReport = new SalesReport
                {
                    ReportDate = StartDate,
                    TotalInvoices = RecentInvoices.Count,
                    TotalSales = RecentInvoices.Sum(i => i.TotalAmount),
                    TotalTax = RecentInvoices.Sum(i => i.TaxAmount),
                    TotalDiscount = RecentInvoices.Sum(i => i.DiscountAmount),
                    TopSellingItems = RecentInvoices.SelectMany(i => i.Items)
                        .GroupBy(it => it.MedicineName)
                        .Select(g => new TopSellingItem { MedicineName = g.Key, QuantitySold = g.Sum(i => i.Quantity), TotalRevenue = g.Sum(i => i.TotalPrice) })
                        .OrderByDescending(t => t.QuantitySold).Take(10).ToList()
                };
            }

            ShowSuccess("Invoice deleted successfully.");
        }
        catch (Exception ex)
        {
            ShowError($"Error deleting invoice: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public bool HasInventoryReport
    {
        get => _hasInventoryReport;
        set => SetProperty(ref _hasInventoryReport, value);
    }

    public bool HasExpiryReport
    {
        get => _hasExpiryReport;
        set => SetProperty(ref _hasExpiryReport, value);
    }

    public bool HasProfitReport
    {
        get => _hasProfitReport;
        set => SetProperty(ref _hasProfitReport, value);
    }

    public ReportsViewModel(IUnitOfWork unitOfWork, IReportService reportService, IExportService exportService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        Title = "Reports";
    }

    [RelayCommand]
    private async Task GenerateDailySalesReportAsync()
    {
        try
        {
            IsBusy = true;
            // Generate sales report for the selected single day (StartDate)
            var start = StartDate.Date;
            var end = start.AddDays(1).AddTicks(-1);
            await GenerateSalesRangeReportAsync(start, end);
            HasSalesReport = SalesReport != null && SalesReport.TotalInvoices > 0;
        }
        catch (Exception ex)
        {
            ShowError($"Report error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateMonthlySalesReportAsync()
    {
        try
        {
            IsBusy = true;
            // Use the selected StartDate/EndDate range for monthly-like report
            var start = StartDate.Date;
            var end = EndDate.Date.AddDays(1).AddTicks(-1);
            await GenerateSalesRangeReportAsync(start, end);
            HasSalesReport = SalesReport != null && SalesReport.TotalInvoices > 0;
        }
        catch (Exception ex)
        {
            ShowError($"Report error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task GenerateSalesRangeReportAsync(DateTime start, DateTime end)
    {
        // Build a SalesReport from invoice data in range
        var invoices = (await _unitOfWork.Invoices.GetByDateRangeAsync(start, end)).Where(i => i.Status == InvoiceStatus.Paid).ToList();

        var report = new SalesReport
        {
            ReportDate = start,
            TotalInvoices = invoices.Count,
            TotalSales = invoices.Sum(i => i.TotalAmount),
            TotalTax = invoices.Sum(i => i.TaxAmount),
            TotalDiscount = invoices.Sum(i => i.DiscountAmount)
        };

        report.PaymentSummaries = invoices.GroupBy(i => i.PaymentMethod)
            .Select(g => new PaymentMethodSummary
            {
                Method = g.Key,
                Count = g.Count(),
                Total = g.Sum(i => i.TotalAmount)
            }).ToList();

        report.TopSellingItems = invoices.SelectMany(i => i.Items)
            .GroupBy(it => it.MedicineName)
            .Select(g => new TopSellingItem
            {
                MedicineName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity),
                TotalRevenue = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(t => t.QuantitySold)
            .Take(10)
            .ToList();

        SalesReport = report;
    }

    [RelayCommand]
    private async Task GenerateInventoryReportAsync()
    {
        try
        {
            IsBusy = true;
            InventoryReport = await _reportService.GetInventoryReportAsync();
            HasInventoryReport = InventoryReport != null && InventoryReport.TotalMedicines > 0;
        }
        catch (Exception ex)
        {
            ShowError($"Report error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateExpiryReportAsync()
    {
        try
        {
            IsBusy = true;
            ExpiryReport = await _reportService.GetExpiryReportAsync(30);
        }
        catch (Exception ex)
        {
            ShowError($"Report error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateSupplierReportAsync()
    {
        try
        {
            IsBusy = true;
            SupplierReport = await _reportService.GetSupplierReportAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Report error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GenerateProfitReportAsync()
    {
        try
        {
            IsBusy = true;
            ProfitReport = await _reportService.GetProfitReportAsync(StartDate, EndDate);
        }
        catch (Exception ex)
        {
            ShowError($"Report error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadRecentInvoicesAsync()
    {
        try
        {
            IsBusy = true;
            var invoices = await _unitOfWork.Invoices.GetByDateRangeAsync(StartDate, EndDate);
            RecentInvoices = new ObservableCollection<Invoice>(invoices);
            // update SalesReport preview values when invoices loaded
            if (invoices != null && invoices.Any())
            {
                SalesReport = new SalesReport
                {
                    ReportDate = StartDate,
                    TotalInvoices = invoices.Count(),
                    TotalSales = invoices.Sum(i => i.TotalAmount),
                    TotalTax = invoices.Sum(i => i.TaxAmount),
                    TotalDiscount = invoices.Sum(i => i.DiscountAmount),
                    TopSellingItems = invoices.SelectMany(i => i.Items)
                        .GroupBy(it => it.MedicineName)
                        .Select(g => new TopSellingItem { MedicineName = g.Key, QuantitySold = g.Sum(i => i.Quantity), TotalRevenue = g.Sum(i => i.TotalPrice) })
                        .OrderByDescending(t => t.QuantitySold).Take(10).ToList()
                };
                HasSalesReport = true;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error loading invoices: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportToExcelAsync()
    {
        await ExportAsync<Invoice>("Excel", "xlsx", async data => await _exportService.ExportToExcelAsync(data, "Report"));
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        await ExportAsync<Invoice>("CSV", "csv", async data => await _exportService.ExportToCsvAsync(data));
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        await ExportAsync<Invoice>("PDF", "pdf", async data => await _exportService.ExportToPdfAsync(data, "Report"));
    }

    private async Task ExportAsync<T>(
        string filterName,
        string extension,
        Func<List<T>, Task<byte[]>> exportFunc) where T : class
    {
        try
        {
            IsBusy = true;

            var saveDialog = new SaveFileDialog
            {
                Filter = $"{filterName} files (*.{extension})|*.{extension}",
                FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}"
            };

            if (saveDialog.ShowDialog() != true)
                return;

            var data = await GetReportDataAsync<T>();

            if (data == null || data.Count == 0)
            {
                ShowError("No data available to export.");
                return;
            }

            var bytes = await exportFunc(data);

            if (bytes == null || bytes.Length == 0)
            {
                ShowError("Export generated an empty file.");
                return;
            }

            await File.WriteAllBytesAsync(saveDialog.FileName, bytes);

            var fileInfo = new FileInfo(saveDialog.FileName);

            ShowSuccess(
                $"Export completed.\n\n" +
                $"File: {fileInfo.FullName}\n" +
                $"Size: {fileInfo.Length:N0} bytes");
        }
        catch (Exception ex)
        {
            ShowError(
                $"Export Failed\n\n" +
                $"Message: {ex.Message}\n\n" +
                $"Details:\n{ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }
    private List<T>? GetReportData<T>() where T : class
    {
        if (typeof(T) == typeof(Invoice) && RecentInvoices.Count > 0)
            return RecentInvoices.Cast<T>().ToList();

        if (typeof(T) == typeof(Medicine) && InventoryReport?.LowStockItems != null)
            return InventoryReport.LowStockItems.Cast<T>().ToList();

        return null;
    }

    private async Task<List<T>?> GetReportDataAsync<T>() where T : class
    {
        // Invoice export: prefer RecentInvoices, otherwise load from date range
        if (typeof(T) == typeof(Invoice))
        {
            if (RecentInvoices != null && RecentInvoices.Count > 0)
                return RecentInvoices.Cast<T>().ToList();

            var invoices = await _unitOfWork.Invoices.GetByDateRangeAsync(StartDate, EndDate);
            if (invoices != null && invoices.Any())
                return invoices.Cast<T>().ToList();

            return null;
        }

        // Medicine export (low stock)
        if (typeof(T) == typeof(Medicine))
        {
            if (InventoryReport?.LowStockItems != null && InventoryReport.LowStockItems.Any())
                return InventoryReport.LowStockItems.Cast<T>().ToList();

            var inv = await _reportService.GetInventoryReportAsync();
            if (inv.LowStockItems != null && inv.LowStockItems.Any())
                return inv.LowStockItems.Cast<T>().ToList();

            return null;
        }

        // TopSellingItem export
        if (typeof(T) == typeof(TopSellingItem) || typeof(T).Name == nameof(TopSellingItem))
        {
            if (SalesReport?.TopSellingItems != null && SalesReport.TopSellingItems.Any())
                return SalesReport.TopSellingItems.Cast<T>().ToList();

            // Try to get report for selected StartDate, fallback to today
            var target = StartDate == default ? DateTime.UtcNow : StartDate;
            var sr = await _reportService.GetDailySalesReportAsync(target);
            if (sr.TopSellingItems != null && sr.TopSellingItems.Any())
                return sr.TopSellingItems.Cast<T>().ToList();

            return null;
        }

        return GetReportData<T>();
    }
}
