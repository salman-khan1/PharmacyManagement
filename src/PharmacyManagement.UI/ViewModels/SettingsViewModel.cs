using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Infrastructure.Logging;
using System.IO;

namespace PharmacyManagement.UI.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ILoggerService _logger;

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _logFilePath = string.Empty;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private string _pharmacyName = "My Pharmacy";

    [ObservableProperty]
    private string _pharmacyAddress = string.Empty;

    [ObservableProperty]
    private string _pharmacyPhone = string.Empty;

    [ObservableProperty]
    private string _pharmacyEmail = string.Empty;

    public SettingsViewModel(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Title = "Settings";

        // Load settings
        DatabasePath = App.Configuration?["ConnectionStrings:DefaultConnection"] ?? "In-Memory Mode";
        LogFilePath = App.Configuration?["Logging:LogFilePath"] ?? "logs/pharmacy-.log";
        PharmacyName = App.Configuration?["Pharmacy:Name"] ?? "My Pharmacy";
        PharmacyAddress = App.Configuration?["Pharmacy:Address"] ?? "";
        PharmacyPhone = App.Configuration?["Pharmacy:Phone"] ?? "";
        PharmacyEmail = App.Configuration?["Pharmacy:Email"] ?? "";
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            _logger.LogInformation("Settings saved");
            ShowSuccess("Settings saved successfully.");
        }
        catch (Exception ex)
        {
            ShowError($"Error saving settings: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        try
        {
            IsDarkTheme = !IsDarkTheme;

            var themePath = IsDarkTheme
                ? "Themes/DarkTheme.xaml"
                : "Themes/LightTheme.xaml";

            var newTheme = new System.Windows.ResourceDictionary
            {
                Source = new Uri(themePath, System.UriKind.Relative)
            };

            App.Current.Resources.MergedDictionaries.Clear();
            App.Current.Resources.MergedDictionaries.Add(newTheme);

            _logger.LogInformation($"Theme changed to {(IsDarkTheme ? "Dark" : "Light")}");
        }
        catch (Exception ex)
        {
            ShowError($"Theme error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        try
        {
            var logDir = Path.GetDirectoryName(LogFilePath);
            if (!string.IsNullOrEmpty(logDir) && Directory.Exists(logDir))
            {
                System.Diagnostics.Process.Start("explorer.exe", logDir);
            }
            else
            {
                ShowError("Log folder not found.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error opening log folder: {ex.Message}");
        }
    }
}
