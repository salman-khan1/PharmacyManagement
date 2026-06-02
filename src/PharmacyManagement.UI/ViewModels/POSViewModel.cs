using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;
using PharmacyManagement.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Diagnostics;
using System.Drawing.Printing;
using PharmacyManagement.Infrastructure.Export;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PharmacyManagement.UI.ViewModels;

public partial class POSViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISalesService _salesService;
    private readonly IExportService _exportService;
    private CancellationTokenSource? _searchCts;

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

    public POSViewModel(IUnitOfWork unitOfWork, ISalesService salesService, IExportService exportService)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _salesService = salesService ?? throw new ArgumentNullException(nameof(salesService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        Title = "Point of Sale";
    }

public class InvoiceItemDto
{
    public string Medicine { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
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
        await SearchMedicineInternalAsync(SearchText, CancellationToken.None);
    }

    // Called by generated observable property partial when SearchText changes
    partial void OnSearchTextChanged(string value)
    {
        // fire-and-forget debounced search
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
            await SearchMedicineInternalAsync(value, token);
        }
        catch (OperationCanceledException) { }
        catch { }
    }

    private async Task SearchMedicineInternalAsync(string searchText, CancellationToken token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                await Application.Current.Dispatcher.InvokeAsync(() => SearchResults.Clear());
                return;
            }

            // quick barcode exact match priority
            var barcodeMatch = await _unitOfWork.Medicines.GetByBarcodeAsync(searchText);

            var results = (await _unitOfWork.Medicines.SearchAsync(searchText)).ToList();

            // scoring for relevance
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

            // ensure barcode match first
            if (barcodeMatch != null)
            {
                scored.RemoveAll(m => m.Id == barcodeMatch.Id);
                scored.Insert(0, barcodeMatch);
            }

            token.ThrowIfCancellationRequested();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                SearchResults = new ObservableCollection<Medicine>(scored.Take(20));
            });
        }
        catch (OperationCanceledException) { }
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

        // Ensure we listen to property changes on items so UI edits update totals
        foreach (var it in CartItems)
        {
            it.PropertyChanged -= CartItem_PropertyChanged;
            it.PropertyChanged += CartItem_PropertyChanged;
        }

        // Refresh the collection to update UI
        var items = new ObservableCollection<CartItem>(CartItems);
        CartItems = items;

        CalculateTotals();
    }

    private void CartItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CartItem.Quantity) || e.PropertyName == nameof(CartItem.UnitPrice))
        {
            var item = sender as CartItem;
            if (item != null)
            {
                item.Total = item.Quantity * item.UnitPrice;
                CalculateTotals();
            }
        }
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

        // Total will be recalculated by the property changed handler
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

            // Offer to print
            var result = MessageBox.Show("Do you want to print the receipt?", "Print Receipt", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await PrintInvoiceAsync(invoice);
            }

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

    [RelayCommand]
    private void IncrementQuantity(CartItem item)
    {
        if (item == null) return;
        item.Quantity++;
    }

    [RelayCommand]
    private void DecrementQuantity(CartItem item)
    {
        if (item == null) return;
        if (item.Quantity > 1)
            item.Quantity--;
        else
            CartItems.Remove(item);
    }

    private async Task PrintInvoiceAsync(Invoice invoice)
    {
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("PHARMACY RECEIPT");
            sb.AppendLine($"Invoice: {invoice.InvoiceNumber}");
            sb.AppendLine($"Date: {invoice.CreatedAt:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"Customer: {invoice.CustomerName}");
            sb.AppendLine(new string('-', 40));
            sb.AppendLine("Item\tQty\tPrice\tTotal");
            foreach (var it in invoice.Items)
            {
                sb.AppendLine($"{it.MedicineName}\t{it.Quantity}\t{it.UnitPrice:C}\t{it.TotalPrice:C}");
            }
            sb.AppendLine(new string('-', 40));
            sb.AppendLine($"Subtotal: {invoice.SubTotal:C}");
            sb.AppendLine($"Discount: {invoice.DiscountAmount:C}");
            sb.AppendLine($"Tax: {invoice.TaxAmount:C}");
            sb.AppendLine($"Total: {invoice.TotalAmount:C}");

            // If no printers installed, skip native print attempt and go to export fallback
            if (System.Drawing.Printing.PrinterSettings.InstalledPrinters.Count == 0)
            {
                MessageBox.Show("No printers installed on this system. The receipt will be saved to a file.", "No Printer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Attempt native print first
                try
                {
                    var flowDoc = new FlowDocument(new Paragraph(new Run(sb.ToString()))) { PagePadding = new Thickness(20) };
                    var pd = new PrintDialog();
                    var fd = (IDocumentPaginatorSource)flowDoc;
                    // If PrintDialog is available and user proceeds, print
                    if (pd.ShowDialog() == true)
                    {
                        pd.PrintDocument(fd.DocumentPaginator, "Invoice Print");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // If platform doesn't support PrintDialog, inform and continue to fallback
                    ShowError($"Printing not available: {ex.Message}");
                }
            }

            // Try using System.Drawing.Printing.PrintDocument (fallback) — prints to default printer
            try
            {
                var pdDoc = new System.Drawing.Printing.PrintDocument();
                if (pdDoc.PrinterSettings != null && pdDoc.PrinterSettings.IsValid)
                {
                    var textToPrint = sb.ToString();
                    pdDoc.PrintPage += (s, e) =>
                    {
                        var font = new System.Drawing.Font("Consolas", 10);
                        var brush = System.Drawing.Brushes.Black;
                        float lineHeight = font.GetHeight(e.Graphics) + 2;
                        float x = e.MarginBounds.Left;
                        float y = e.MarginBounds.Top;
                        using (var sr = new StringReader(textToPrint))
                        {
                            string? line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                e.Graphics.DrawString(line, font, brush, x, y);
                                y += lineHeight;
                                if (y + lineHeight > e.MarginBounds.Bottom)
                                {
                                    e.HasMorePages = true;
                                    return;
                                }
                            }
                        }
                        e.HasMorePages = false;
                    };
                    // Print (may throw if printer unavailable)
                    pdDoc.Print();
                    return;
                }
            }
            catch
            {
                // continue to file export fallback
            }

            // Export fallback: write textual PDF-like file (ExportService produces bytes)
            try
            {
                // Prepare typed DTO list so exporter can reflect properties
                var itemsList = invoice.Items.Select(i => new InvoiceItemDto
                {
                    Medicine = i.MedicineName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Total = i.TotalPrice
                }).ToList();

                var bytes = await _exportService.ExportToPdfAsync(itemsList, "Invoice");

                // If no printers installed, auto-save to Receipts folder
                if (System.Drawing.Printing.PrinterSettings.InstalledPrinters.Count == 0)
                {
                    var receiptsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Receipts");
                    Directory.CreateDirectory(receiptsDir);
                    var path = Path.Combine(receiptsDir, $"Invoice_{invoice.InvoiceNumber}.pdf");
                    await File.WriteAllBytesAsync(path, bytes);
                    MessageBox.Show($"No printers found. Invoice saved to {path}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);

                    try
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                    catch { }

                    return;
                }

                // Otherwise ask user where to save
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    FileName = $"Invoice_{invoice.InvoiceNumber}.pdf"
                };
                if (dlg.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(dlg.FileName, bytes);
                    MessageBox.Show($"Invoice saved to {dlg.FileName}", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Print/Export error: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Print error: {ex.Message}");
        }
    }

    private void CalculateTotals()
    {
        SubTotal = CartItems.Sum(c => c.Total);

        // Support user entering discount as percent (e.g. 10 for 10%) or decimal (0.10)
        var effectiveDiscountRate = GetNormalizedDiscountRate(DiscountRate);

        DiscountAmount = Math.Round(SubTotal * effectiveDiscountRate, 2);
        TaxAmount = Math.Round((SubTotal - DiscountAmount) * TaxRate, 2);
        TotalAmount = Math.Round(SubTotal - DiscountAmount + TaxAmount, 2);
        ChangeAmount = Math.Round(AmountPaid - TotalAmount, 2);
    }

    private static decimal GetNormalizedDiscountRate(decimal rate)
    {
        if (rate <= 0) return 0m;
        // If user typed as percentage like '10' meaning 10%, normalize to 0.10
        if (rate > 1) return rate / 100m;
        return rate;
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
