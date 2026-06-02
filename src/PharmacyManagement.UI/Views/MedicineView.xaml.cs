using System.Windows;
using System.Windows.Controls;

namespace PharmacyManagement.UI.Views;

public partial class MedicineView : UserControl
{
    public MedicineView()
    {
        InitializeComponent();
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ViewModels.MedicineViewModel vm && sender is ComboBox comboBox)
        {
            if (comboBox.SelectedItem is string category)
            {
                vm.FilterByCategoryCommand.Execute(category);
            }
        }
    }
}
