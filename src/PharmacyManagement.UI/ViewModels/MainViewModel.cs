using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Logging;
using PharmacyManagement.Infrastructure.Services;
using System.Windows;

namespace PharmacyManagement.UI.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IReportService _reportService;
    private readonly ILoggerService _logger;

    [ObservableProperty]
    private User? _currentUser;

    [ObservableProperty]
    private BaseViewModel? _selectedViewModel;

    [ObservableProperty]
    private DashboardViewModel _dashboardVM;

    [ObservableProperty]
    private MedicineViewModel _medicineVM;

    [ObservableProperty]
    private InventoryViewModel _inventoryVM;

    [ObservableProperty]
    private POSViewModel _posVM;

    [ObservableProperty]
    private ReportsViewModel _reportsVM;

    [ObservableProperty]
    private SettingsViewModel _settingsVM;

    [ObservableProperty]
    private bool _isLoggedIn = false;

    [ObservableProperty]
    private string _connectionStatus = "Online";

    public MainViewModel(IAuthService authService, IReportService reportService, ILoggerService logger,
        DashboardViewModel dashboardViewModel, MedicineViewModel medicineViewModel,
        InventoryViewModel inventoryViewModel, POSViewModel posViewModel,
        ReportsViewModel reportsViewModel, SettingsViewModel settingsViewModel)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        DashboardVM = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
        MedicineVM = medicineViewModel ?? throw new ArgumentNullException(nameof(medicineViewModel));
        InventoryVM = inventoryViewModel ?? throw new ArgumentNullException(nameof(inventoryViewModel));
        PosVM = posViewModel ?? throw new ArgumentNullException(nameof(posViewModel));
        ReportsVM = reportsViewModel ?? throw new ArgumentNullException(nameof(reportsViewModel));
        SettingsVM = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

        Title = "Pharmacy Management System";
        SelectedViewModel = DashboardVM;

        _logger.LogInformation("MainViewModel initialized");
    }

    [RelayCommand]
    private void NavigateTo(string viewName)
    {
        SelectedViewModel = viewName switch
        {
            "Dashboard" => DashboardVM,
            "Medicines" => MedicineVM,
            "Inventory" => InventoryVM,
            "POS" => PosVM,
            "Reports" => ReportsVM,
            "Settings" => SettingsVM,
            _ => DashboardVM
        };

        Title = $"Pharmacy Management - {viewName}";
        _logger.LogInformation($"Navigated to {viewName}");
    }

    [RelayCommand]
    private void Logout()
    {
        CurrentUser = null;
        IsLoggedIn = false;
        _logger.LogInformation("User logged out");

        // Show login window
        var loginWindow = new Views.LoginWindow();
        loginWindow.Show();

        // Close main window
        Application.Current.Windows.OfType<Window>()
            .FirstOrDefault(w => w is Views.MainWindow)?.Close();
    }

    public void SetCurrentUser(User user)
    {
        CurrentUser = user;
        IsLoggedIn = true;
        _logger.LogInformation($"User {user.Username} logged in with role {user.Role}");
    }
}
