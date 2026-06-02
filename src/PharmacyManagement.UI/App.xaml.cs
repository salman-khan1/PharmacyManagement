using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Infrastructure.Export;
using PharmacyManagement.Infrastructure.Logging;
using PharmacyManagement.Infrastructure.Repositories;
using PharmacyManagement.Infrastructure.Services;
using PharmacyManagement.Persistence.Data;
using PharmacyManagement.UI.ViewModels;
using PharmacyManagement.UI.Views;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace PharmacyManagement.UI;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;
    public static IConfiguration Configuration { get; private set; } = null!;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        try
        {
            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            // Setup DI
            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();

            // Initialize database if configured
            InitializeDatabase();

            // Seed data
            Task.Run(async () =>
            {
                try
                {
                    var seedService = ServiceProvider.GetRequiredService<ISeedService>();
                    await seedService.SeedAsync();
                }
                catch
                {
                    // Seeding is optional - app continues even if it fails
                }
            }).Wait();

            // Show main window
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application startup error: {ex.Message}\n\nThe application will continue in offline mode.",
                "Startup Warning", MessageBoxButton.OK, MessageBoxImage.Warning);

            // Fallback: start with in-memory storage
            StartInOfflineMode();
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration
        services.AddSingleton(Configuration);

        // Logging
        var logPath = Configuration["Logging:LogFilePath"] ?? "logs/pharmacy-.log";
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        services.AddSingleton<ILoggerService>(new LoggerService(logPath));

        // Determine storage mode
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        var useDatabase = !string.IsNullOrWhiteSpace(connectionString);

        if (useDatabase)
        {
            // Database mode
            services.AddDbContext<PharmacyDbContext>(options =>
                options.UseSqlite(connectionString));

            services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        }
        else
        {
            // In-memory mode
            services.AddSingleton<PharmacyDbContext>(sp => null!); // Not used but registered for compatibility
            services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
        }

        // Services
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<ISeedService, SeedService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<MedicineViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<POSViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LoginViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<LoginWindow>();
    }

    private void InitializeDatabase()
    {
        try
        {
            var connectionString = Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString)) return;

            using var scope = ServiceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PharmacyDbContext>();
            context.Database.EnsureCreated();
        }
        catch
        {
            // Database initialization failed - switch to in-memory
            MessageBox.Show("Database connection failed. Switching to offline mode.",
                "Database Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void StartInOfflineMode()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();
        services.AddSingleton<ILoggerService>(new LoggerService("logs/pharmacy-offline-.log"));
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IInventoryService, InventoryService>();
        services.AddSingleton<ISalesService, SalesService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<ISeedService, SeedService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<MedicineViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<POSViewModel>();
        services.AddTransient<ReportsViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<LoginViewModel>();

        services.AddTransient<MainWindow>();

        ServiceProvider = services.BuildServiceProvider();

        // Seed in-memory data
        Task.Run(async () =>
        {
            try
            {
                var seedService = ServiceProvider.GetRequiredService<ISeedService>();
                await seedService.SeedAsync();
            }
            catch { }
        }).Wait();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        try
        {
            var logger = ServiceProvider?.GetService<ILoggerService>();
            logger?.LogError("Unhandled UI exception", e.Exception);
        }
        catch { }

        MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}\n\nPlease try again or contact support.",
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
