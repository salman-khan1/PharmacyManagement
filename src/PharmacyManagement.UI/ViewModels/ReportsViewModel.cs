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

    [ObservableProperty]
    private InventoryReport? _inventoryReport;

    [ObservableProperty]
    private ExpiryReport? _expiryReport;

    [ObservableProperty]
    private SupplierReport? _supplierReport;

    [ObservableProperty]
    private ProfitReport? _profitReport;

    [ObservableProperty]
    private ObservableCollection<Invoice> _recentInvoices = new();

    [ObservableProperty]
    private int _selectedReportIndex;

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
            SalesReport = await _reportService.GetDailySalesReportAsync(DateTime.UtcNow);
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
            SalesReport = await _reportService.GetMonthlySalesReportAsync(DateTime.UtcNow.Year, DateTime.UtcNow.Month);
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
    private async Task GenerateInventoryReportAsync()
    {
        try
        {
            IsBusy = true;
            InventoryReport = await _reportService.GetInventoryReportAsync();
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
        await ExportAsync("Excel", "xlsx", async data => await _exportService.ExportToExcelAsync(data, "Report"));
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        await ExportAsync("CSV", "csv", async data => await _exportService.ExportToCsvAsync(data));
    }

    [RelayCommand]
    private async Task ExportToPdfAsync()
    {
        await ExportAsync("PDF", "pdf", async data => await _exportService.ExportToPdfAsync(data, "Report"));
    }

    private async Task ExportAsync<T>(string filterName, string extension, Func<List<T>, Task<byte[]>> exportFunc) where T : class
    {
        try
        {
            IsBusy = true;

            var saveDialog = new SaveFileDialog
            {
                Filter = $"{filterName} files (*.{extension})|*.{extension}",
                FileName = $"Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}"
            };

            if (saveDialog.ShowDialog() != true) return;

            List<T> data = GetReportData<T>();
            if (data == null || data.Count == 0)
            {
                ShowError("No data to export.");
                return;
            }

            var bytes = await exportFunc(data);
            await File.WriteAllBytesAsync(saveDialog.FileName, bytes);

            ShowSuccess($"Report exported to {saveDialog.FileName}");
        }
        catch (Exception ex)
        {
            ShowError($"Export error: {ex.Message}");
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
}
