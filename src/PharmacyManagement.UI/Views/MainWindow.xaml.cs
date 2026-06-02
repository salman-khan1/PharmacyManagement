using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace PharmacyManagement.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // Resolve ViewModel from DI and assign as DataContext
        DataContext = App.ServiceProvider.GetRequiredService<ViewModels.MainViewModel>();
    }
}
