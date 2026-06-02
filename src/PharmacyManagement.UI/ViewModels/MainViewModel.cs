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
    private DashboardViewModel _dashboardViewModel;

    [ObservableProperty]
    private MedicineViewModel _medicineViewModel;

    [ObservableProperty]
    private InventoryViewModel _inventoryViewModel;

    [ObservableProperty]
    private POSViewModel _posViewModel;

    [ObservableProperty]
    private ReportsViewModel _reportsViewModel;

    [ObservableProperty]
    private SettingsViewModel _settingsViewModel;

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

        DashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));
        MedicineViewModel = medicineViewModel ?? throw new ArgumentNullException(nameof(medicineViewModel));
        InventoryViewModel = inventoryViewModel ?? throw new ArgumentNullException(nameof(inventoryViewModel));
        POSViewModel = posViewModel ?? throw new ArgumentNullException(nameof(posViewModel));
        ReportsViewModel = reportsViewModel ?? throw new ArgumentNullException(nameof(reportsViewModel));
        SettingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

        Title = "Pharmacy Management System";
        SelectedViewModel = DashboardViewModel;

        _logger.LogInformation("MainViewModel initialized");
    }

    [RelayCommand]
    private void NavigateTo(string viewName)
    {
        SelectedViewModel = viewName switch
        {
            "Dashboard" => DashboardViewModel,
            "Medicines" => MedicineViewModel,
            "Inventory" => InventoryViewModel,
            "POS" => POSViewModel,
            "Reports" => ReportsViewModel,
            "Settings" => SettingsViewModel,
            _ => DashboardViewModel
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
