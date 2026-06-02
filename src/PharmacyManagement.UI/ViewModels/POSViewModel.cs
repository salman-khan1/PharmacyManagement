using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Services;
using System.Collections.ObjectModel;

namespace PharmacyManagement.UI.ViewModels;

public partial class POSViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISalesService _salesService;

    [ObservableProperty]
    private ObservableCollection<CartItem> _cartItems = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _barcodeText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Medicine> _searchResults = new();

    [ObservableProperty]
    private string _customerName = "Walk-in Customer";

    [ObservableProperty]
    private string _customerPhone = string.Empty;

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [ObservableProperty]
    private decimal _subTotal;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _discountRate;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private decimal _amountPaid;

    [ObservableProperty]
    private decimal _changeAmount;

    [ObservableProperty]
    private decimal _taxRate = 0.05m;

    public POSViewModel(IUnitOfWork unitOfWork, ISalesService salesService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _salesService = salesService ?? throw new ArgumentNullException(nameof(salesService));
        Title = "Point of Sale";
    }

    partial void OnDiscountRateChanged(decimal value)
    {
        CalculateTotals();
    }

    partial void OnAmountPaidChanged(decimal value)
    {
        CalculateTotals();
    }

    [RelayCommand]
    private async Task SearchMedicineAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchResults.Clear();
                return;
            }

            var results = await _unitOfWork.Medicines.SearchAsync(SearchText);
            SearchResults = new ObservableCollection<Medicine>(results.Take(10));
        }
        catch (Exception ex)
        {
            ShowError($"Search error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task ScanBarcodeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BarcodeText)) return;

            var medicine = await _unitOfWork.Medicines.GetByBarcodeAsync(BarcodeText);
            if (medicine != null)
            {
                AddToCart(medicine);
                BarcodeText = string.Empty;
            }
            else
            {
                ShowError("Medicine not found.");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Scan error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddToCartFromSearch(Medicine medicine)
    {
        if (medicine == null) return;
        AddToCart(medicine);
        SearchText = string.Empty;
        SearchResults.Clear();
    }

    private void AddToCart(Medicine medicine)
    {
        if (medicine.Quantity <= 0)
        {
            ShowError("This medicine is out of stock.");
            return;
        }

        var existingItem = CartItems.FirstOrDefault(c => c.MedicineId == medicine.Id);
        if (existingItem != null)
        {
            if (existingItem.Quantity >= medicine.Quantity)
            {
                ShowError("Not enough stock available.");
                return;
            }
            existingItem.Quantity++;
            existingItem.Total = existingItem.Quantity * existingItem.UnitPrice;
        }
        else
        {
            CartItems.Add(new CartItem
            {
                MedicineId = medicine.Id,
                MedicineName = medicine.MedicineName,
                GenericName = medicine.GenericName,
                Quantity = 1,
                UnitPrice = medicine.SellingPrice,
                Total = medicine.SellingPrice,
                BatchNumber = medicine.BatchNumber
            });
        }

        // Refresh the collection to update UI
        var items = new ObservableCollection<CartItem>(CartItems);
        CartItems = items;

        CalculateTotals();
    }

    [RelayCommand]
    private void RemoveFromCart(CartItem item)
    {
        if (item == null) return;
        CartItems.Remove(item);
        CalculateTotals();
    }

    [RelayCommand]
    private void UpdateQuantity(CartItem item)
    {
        if (item == null || item.Quantity <= 0)
        {
            if (item != null) CartItems.Remove(item);
            CalculateTotals();
            return;
        }

        item.Total = item.Quantity * item.UnitPrice;
        CalculateTotals();

        // Refresh
        var items = new ObservableCollection<CartItem>(CartItems);
        CartItems = items;
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        try
        {
            if (CartItems.Count == 0)
            {
                ShowError("Cart is empty.");
                return;
            }

            if (AmountPaid < TotalAmount)
            {
                ShowError("Amount paid is less than total amount.");
                return;
            }

            IsBusy = true;

            var items = CartItems.Select(c => (c.MedicineId, c.Quantity, 0m as decimal?)).ToList();
            var invoice = await _salesService.CreateInvoiceAsync(
                CustomerName, CustomerPhone, SelectedPaymentMethod, items, DiscountRate);

            ShowSuccess($"Sale completed! Invoice: {invoice.InvoiceNumber}\nTotal: {invoice.TotalAmount:C}");

            // Clear cart
            CartItems.Clear();
            CalculateTotals();
            CustomerName = "Walk-in Customer";
            CustomerPhone = string.Empty;
            AmountPaid = 0;
            DiscountRate = 0;
        }
        catch (Exception ex)
        {
            ShowError($"Checkout error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearCart()
    {
        CartItems.Clear();
        CalculateTotals();
        CustomerName = "Walk-in Customer";
        CustomerPhone = string.Empty;
        AmountPaid = 0;
        DiscountRate = 0;
    }

    private void CalculateTotals()
    {
        SubTotal = CartItems.Sum(c => c.Total);
        DiscountAmount = SubTotal * DiscountRate;
        TaxAmount = (SubTotal - DiscountAmount) * TaxRate;
        TotalAmount = SubTotal - DiscountAmount + TaxAmount;
        ChangeAmount = AmountPaid - TotalAmount;
    }
}

public partial class CartItem : ObservableObject
{
    [ObservableProperty]
    private Guid _medicineId;

    [ObservableProperty]
    private string _medicineName = string.Empty;

    [ObservableProperty]
    private string _genericName = string.Empty;

    [ObservableProperty]
    private string _batchNumber = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private decimal _total;
}
