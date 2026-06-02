using CommunityToolkit.Mvvm.ComponentModel;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Services;
using System.Collections.ObjectModel;

namespace PharmacyManagement.UI.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IReportService _reportService;

    [ObservableProperty]
    private int _totalMedicines;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _expiringSoonCount;

    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private int _todayInvoices;

    [ObservableProperty]
    private ObservableCollection<Medicine> _lowStockItems = new();

    [ObservableProperty]
    private ObservableCollection<Medicine> _expiringItems = new();

    public DashboardViewModel(IUnitOfWork unitOfWork, IReportService reportService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));

        Title = "Dashboard";
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;

            var medicines = await _unitOfWork.Medicines.GetAllAsync();
            var medList = medicines.ToList();
            TotalMedicines = medList.Count;

            var lowStock = await _unitOfWork.Medicines.GetLowStockAsync();
            var lowStockList = lowStock.ToList();
            LowStockCount = lowStockList.Count;
            LowStockItems = new ObservableCollection<Medicine>(lowStockList.Take(5));

            var expiring = await _unitOfWork.Medicines.GetExpiringSoonAsync(30);
            var expiringList = expiring.ToList();
            ExpiringSoonCount = expiringList.Count;
            ExpiringItems = new ObservableCollection<Medicine>(expiringList.Take(5));

            var todayReport = await _reportService.GetDailySalesReportAsync(DateTime.UtcNow);
            TodaySales = todayReport.TotalSales;
            TodayInvoices = todayReport.TotalInvoices;
        }
        catch (Exception ex)
        {
            ShowError($"Error loading dashboard data: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
