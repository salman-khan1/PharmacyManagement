using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Services;
using System.Collections.ObjectModel;

namespace PharmacyManagement.UI.ViewModels;

public partial class InventoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInventoryService _inventoryService;

    [ObservableProperty]
    private ObservableCollection<Medicine> _lowStockItems = new();

    [ObservableProperty]
    private ObservableCollection<Medicine> _expiringItems = new();

    [ObservableProperty]
    private ObservableCollection<Medicine> _expiredItems = new();

    [ObservableProperty]
    private ObservableCollection<StockTransaction> _recentTransactions = new();

    [ObservableProperty]
    private Medicine? _selectedMedicine;

    [ObservableProperty]
    private int _stockQuantity;

    [ObservableProperty]
    private string _transactionReason = string.Empty;

    [ObservableProperty]
    private string _referenceNumber = string.Empty;

    [ObservableProperty]
    private string _supplier = string.Empty;

    [ObservableProperty]
    private decimal _unitPrice;

    public InventoryViewModel(IUnitOfWork unitOfWork, IInventoryService inventoryService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        Title = "Inventory Management";
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            IsBusy = true;

            var lowStock = await _inventoryService.GetLowStockMedicinesAsync();
            LowStockItems = new ObservableCollection<Medicine>(lowStock);

            var expiring = await _inventoryService.GetExpiringMedicinesAsync(30);
            ExpiringItems = new ObservableCollection<Medicine>(expiring);

            var expired = await _inventoryService.GetExpiredMedicinesAsync();
            ExpiredItems = new ObservableCollection<Medicine>(expired);

            var transactions = await _inventoryService.GetTransactionHistoryAsync();
            RecentTransactions = new ObservableCollection<StockTransaction>(transactions.Take(50));
        }
        catch (Exception ex)
        {
            ShowError($"Error loading inventory: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StockInAsync(Medicine medicine)
    {
        try
        {
            if (medicine == null || StockQuantity <= 0)
            {
                ShowError("Please select a medicine and enter a valid quantity.");
                return;
            }

            IsBusy = true;
            await _inventoryService.StockInAsync(medicine.Id, StockQuantity, UnitPrice, Supplier, TransactionReason, ReferenceNumber);
            ShowSuccess($"Added {StockQuantity} units to {medicine.MedicineName}");
            await LoadDataAsync();
            ClearInputs();
        }
        catch (Exception ex)
        {
            ShowError($"Stock in error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task StockOutAsync(Medicine medicine)
    {
        try
        {
            if (medicine == null || StockQuantity <= 0)
            {
                ShowError("Please select a medicine and enter a valid quantity.");
                return;
            }

            IsBusy = true;
            await _inventoryService.StockOutAsync(medicine.Id, StockQuantity, TransactionReason, ReferenceNumber);
            ShowSuccess($"Removed {StockQuantity} units from {medicine.MedicineName}");
            await LoadDataAsync();
            ClearInputs();
        }
        catch (Exception ex)
        {
            ShowError($"Stock out error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AdjustStockAsync(Medicine medicine)
    {
        try
        {
            if (medicine == null || StockQuantity < 0)
            {
                ShowError("Please select a medicine and enter a valid quantity.");
                return;
            }

            IsBusy = true;
            await _inventoryService.AdjustStockAsync(medicine.Id, StockQuantity, TransactionReason);
            ShowSuccess($"Stock adjusted for {medicine.MedicineName} to {StockQuantity} units");
            await LoadDataAsync();
            ClearInputs();
        }
        catch (Exception ex)
        {
            ShowError($"Adjust error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task MarkDamagedAsync(Medicine medicine)
    {
        try
        {
            if (medicine == null || StockQuantity <= 0)
            {
                ShowError("Please select a medicine and enter a valid quantity.");
                return;
            }

            IsBusy = true;
            await _inventoryService.MarkDamagedAsync(medicine.Id, StockQuantity, TransactionReason);
            ShowSuccess($"Marked {StockQuantity} units of {medicine.MedicineName} as damaged");
            await LoadDataAsync();
            ClearInputs();
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReturnStockAsync(Medicine medicine)
    {
        try
        {
            if (medicine == null || StockQuantity <= 0)
            {
                ShowError("Please select a medicine and enter a valid quantity.");
                return;
            }

            IsBusy = true;
            await _inventoryService.ReturnStockAsync(medicine.Id, StockQuantity, Supplier, TransactionReason);
            ShowSuccess($"Returned {StockQuantity} units of {medicine.MedicineName}");
            await LoadDataAsync();
            ClearInputs();
        }
        catch (Exception ex)
        {
            ShowError($"Return error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadDataAsync();
    }

    private void ClearInputs()
    {
        StockQuantity = 0;
        TransactionReason = string.Empty;
        ReferenceNumber = string.Empty;
        Supplier = string.Empty;
        UnitPrice = 0;
    }
}
