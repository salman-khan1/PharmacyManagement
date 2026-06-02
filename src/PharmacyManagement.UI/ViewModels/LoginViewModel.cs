using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Logging;
using PharmacyManagement.Infrastructure.Services;
using PharmacyManagement.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace PharmacyManagement.UI.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ILoggerService _logger;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isLoginSuccessful;

    [ObservableProperty]
    private User? _loggedInUser;

    public LoginViewModel(IAuthService authService, ILoggerService logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Title = "Login";
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ShowError("Please enter your username.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Please enter your password.");
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            var user = await _authService.AuthenticateAsync(Username, Password);

            if (user != null)
            {
                IsLoginSuccessful = true;
                LoggedInUser = user;
                _logger.LogInformation($"User {Username} logged in successfully");

                // Open main window
                var mainWindow = App.ServiceProvider.GetRequiredService<MainWindow>();
                var mainViewModel = (MainViewModel)mainWindow.DataContext;
                mainViewModel.SetCurrentUser(user);

                mainWindow.Show();

                // Close login window
                Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w is LoginWindow)?.Close();
            }
            else
            {
                IsLoginSuccessful = false;
                ShowError("Invalid username or password.");
                _logger.LogWarning($"Failed login attempt for user {Username}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Login error: {ex.Message}");
            _logger.LogError("Login error", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        Application.Current.Shutdown();
    }
}
