using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PharmacyManagement.UI.ViewModels;

public partial class MedicineViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private ObservableCollection<Medicine> _medicines = new();

    [ObservableProperty]
    private Medicine? _selectedMedicine;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private ObservableCollection<string> _categories = new();

    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private Medicine _newMedicine = new();

    public MedicineViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        Title = "Medicine Management";
        _ = LoadDataAsync();
    }

    private CancellationTokenSource? _searchCts;

    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;

            var medicines = await _unitOfWork.Medicines.GetAllAsync();
            Medicines = new ObservableCollection<Medicine>(medicines);

            var cats = await _unitOfWork.Medicines.GetAllCategoriesAsync();
            Categories = new ObservableCollection<string>(cats.Prepend("All"));
        }
        catch (Exception ex)
        {
            ShowError($"Error loading medicines: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await SearchAsyncInternal(SearchText, CancellationToken.None);
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = Task.Run(async () => await DebouncedSearchAsync(value));
    }

    private async Task DebouncedSearchAsync(string value)
    {
        try
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;
            await Task.Delay(300, token);
            await SearchAsyncInternal(value, token);
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private async Task SearchAsyncInternal(string searchText, CancellationToken token)
    {
        try
        {
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                await LoadDataAsync();
                return;
            }

            // prioritize barcode exact match
            var barcodeMatch = await _unitOfWork.Medicines.GetByBarcodeAsync(searchText);
            var results = (await _unitOfWork.Medicines.SearchAsync(searchText)).ToList();

            var lower = searchText.ToLower();
            var scored = results.Select(m => new
            {
                Med = m,
                Score = (m.Quantity > 0 ? 100 : 0)
                        + (m.MedicineName?.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) == true ? 50 : 0)
                        + (m.MedicineName?.ToLower().Contains(lower) == true ? 20 : 0)
                        + (m.GenericName?.ToLower().Contains(lower) == true ? 10 : 0)
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Med.MedicineName)
            .Select(x => x.Med)
            .ToList();

            if (barcodeMatch != null)
            {
                scored.RemoveAll(m => m.Id == barcodeMatch.Id);
                scored.Insert(0, barcodeMatch);
            }

            token.ThrowIfCancellationRequested();

            Medicines = new ObservableCollection<Medicine>(scored);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            ShowError($"Search error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync(string category)
    {
        try
        {
            IsBusy = true;
            SelectedCategory = category;

            if (category == "All")
            {
                await LoadDataAsync();
                return;
            }

            var results = await _unitOfWork.Medicines.GetByCategoryAsync(category);
            Medicines = new ObservableCollection<Medicine>(results);
        }
        catch (Exception ex)
        {
            ShowError($"Filter error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void AddNew()
    {
        NewMedicine = new Medicine
        {
            CreatedAt = DateTime.UtcNow,
            MinimumQuantity = 10
        };
        IsEditing = true;
    }

    [RelayCommand]
    private void Edit(Medicine medicine)
    {
        if (medicine == null) return;

        NewMedicine = new Medicine
        {
            Id = medicine.Id,
            Barcode = medicine.Barcode,
            MedicineName = medicine.MedicineName,
            GenericName = medicine.GenericName,
            BrandName = medicine.BrandName,
            Category = medicine.Category,
            BatchNumber = medicine.BatchNumber,
            PurchasePrice = medicine.PurchasePrice,
            SellingPrice = medicine.SellingPrice,
            Quantity = medicine.Quantity,
            MinimumQuantity = medicine.MinimumQuantity,
            RackNumber = medicine.RackNumber,
            ShelfNumber = medicine.ShelfNumber,
            ExactLocation = medicine.ExactLocation,
            ExpiryDate = medicine.ExpiryDate,
            ManufacturingDate = medicine.ManufacturingDate,
            Supplier = medicine.Supplier,
            PrescriptionRequired = medicine.PrescriptionRequired,
            Description = medicine.Description,
            ImagePath = medicine.ImagePath,
            CreatedAt = medicine.CreatedAt
        };
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsBusy = true;

            if (string.IsNullOrWhiteSpace(NewMedicine.MedicineName))
            {
                ShowError("Medicine name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(NewMedicine.Barcode))
            {
                ShowError("Barcode is required.");
                return;
            }

            var existing = await _unitOfWork.Medicines.GetByIdAsync(NewMedicine.Id);
            if (existing == null)
            {
                // Add new
                NewMedicine.CreatedAt = DateTime.UtcNow;
                await _unitOfWork.Medicines.AddAsync(NewMedicine);
                ShowSuccess("Medicine added successfully.");
            }
            else
            {
                // Update
                NewMedicine.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Medicines.UpdateAsync(NewMedicine);
                ShowSuccess("Medicine updated successfully.");
            }

            await _unitOfWork.SaveChangesAsync();
            IsEditing = false;
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Save error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(Medicine medicine)
    {
        try
        {
            if (medicine == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Are you sure you want to delete {medicine.MedicineName}?",
                "Confirm Delete", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            IsBusy = true;
            medicine.IsDeleted = true;
            medicine.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Medicines.UpdateAsync(medicine);
            await _unitOfWork.SaveChangesAsync();

            ShowSuccess("Medicine deleted successfully.");
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            ShowError($"Delete error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        NewMedicine = new Medicine();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        SearchText = string.Empty;
        await LoadDataAsync();
    }
}
