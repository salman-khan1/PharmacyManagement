using PharmacyManagement.Domain.Enums;
using PharmacyManagement.Domain.Interfaces;
using PharmacyManagement.Domain.Models;

namespace PharmacyManagement.Infrastructure.Services;

public interface ISalesService
{
    Task<Invoice> CreateInvoiceAsync(string customerName, string customerPhone, PaymentMethod paymentMethod,
        List<(Guid MedicineId, int Quantity, decimal? Discount)> items, decimal? discountRate = null);
    Task<Invoice?> GetInvoiceAsync(Guid invoiceId);
    Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber);
    Task<IEnumerable<Invoice>> GetInvoicesAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Invoice> RefundAsync(Guid invoiceId, List<(Guid ItemId, int Quantity)> refundItems);
    Task<decimal> GetDailySalesAsync(DateTime? date = null);
    Task<decimal> GetMonthlySalesAsync(int? year = null, int? month = null);
}

public class SalesService : ISalesService
{
    private readonly IUnitOfWork _unitOfWork;

    public SalesService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<Invoice> CreateInvoiceAsync(string customerName, string customerPhone, PaymentMethod paymentMethod,
        List<(Guid MedicineId, int Quantity, decimal? Discount)> items, decimal? discountRate = null)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("At least one item is required", nameof(items));

        var invoice = new Invoice
        {
            InvoiceNumber = GenerateInvoiceNumber(),
            CustomerName = customerName ?? "Walk-in Customer",
            CustomerPhone = customerPhone ?? string.Empty,
            PaymentMethod = paymentMethod,
            Status = InvoiceStatus.Paid,
            TaxRate = 0.05m,
            DiscountRate = discountRate ?? 0,
            CreatedAt = DateTime.UtcNow
        };

        decimal subTotal = 0;

        foreach (var (medicineId, quantity, discount) in items)
        {
            if (quantity <= 0) continue;

            var medicine = await _unitOfWork.Medicines.GetByIdAsync(medicineId);
            if (medicine == null) continue;
            if (medicine.Quantity < quantity)
                throw new InvalidOperationException($"Insufficient stock for {medicine.MedicineName}");

            var itemDiscount = discount ?? 0;
            var itemTotal = (quantity * medicine.SellingPrice) - itemDiscount;

            var invoiceItem = new InvoiceItem
            {
                MedicineId = medicineId,
                MedicineName = medicine.MedicineName,
                BatchNumber = medicine.BatchNumber,
                Quantity = quantity,
                UnitPrice = medicine.SellingPrice,
                Discount = itemDiscount,
                TotalPrice = itemTotal,
                CreatedAt = DateTime.UtcNow
            };

            invoice.Items.Add(invoiceItem);
            subTotal += itemTotal;

            // Deduct stock
            medicine.Quantity -= quantity;
            medicine.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Medicines.UpdateAsync(medicine);

            // Record stock transaction
            var stockTransaction = new StockTransaction
            {
                MedicineId = medicineId,
                TransactionType = StockTransactionType.Sale,
                Quantity = quantity,
                UnitPrice = medicine.SellingPrice,
                TotalPrice = itemTotal,
                ReferenceNumber = invoice.InvoiceNumber,
                Reason = $"Sale - Invoice {invoice.InvoiceNumber}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.StockTransactions.AddAsync(stockTransaction);
        }

        invoice.SubTotal = subTotal;
        invoice.DiscountAmount = subTotal * invoice.DiscountRate;
        invoice.TaxAmount = (subTotal - invoice.DiscountAmount) * invoice.TaxRate;
        invoice.TotalAmount = subTotal - invoice.DiscountAmount + invoice.TaxAmount;
        invoice.AmountPaid = invoice.TotalAmount;

        await _unitOfWork.Invoices.AddAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<Invoice?> GetInvoiceAsync(Guid invoiceId)
    {
        return await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
    }

    public async Task<Invoice?> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        return await _unitOfWork.Invoices.GetByInvoiceNumberAsync(invoiceNumber);
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;
        return await _unitOfWork.Invoices.GetByDateRangeAsync(start, end);
    }

    public async Task<Invoice> RefundAsync(Guid invoiceId, List<(Guid ItemId, int Quantity)> refundItems)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice == null) throw new InvalidOperationException("Invoice not found");
        if (invoice.Status == InvoiceStatus.Refunded)
            throw new InvalidOperationException("Invoice already refunded");

        foreach (var (itemId, quantity) in refundItems)
        {
            if (quantity <= 0) continue;

            var item = invoice.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null || item.Quantity < quantity) continue;

            // Restore stock
            var medicine = await _unitOfWork.Medicines.GetByIdAsync(item.MedicineId);
            if (medicine != null)
            {
                medicine.Quantity += quantity;
                medicine.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.Medicines.UpdateAsync(medicine);
            }

            // Record refund transaction
            var stockTransaction = new StockTransaction
            {
                MedicineId = item.MedicineId,
                TransactionType = StockTransactionType.Refund,
                Quantity = quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = quantity * item.UnitPrice,
                ReferenceNumber = invoice.InvoiceNumber,
                Reason = $"Refund - Invoice {invoice.InvoiceNumber}",
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.StockTransactions.AddAsync(stockTransaction);

            if (item.Quantity == quantity)
            {
                invoice.Items.Remove(item);
            }
            else
            {
                item.Quantity -= quantity;
                item.TotalPrice = item.Quantity * item.UnitPrice - item.Discount;
            }
        }

        // Recalculate totals
        invoice.SubTotal = invoice.Items.Sum(i => i.TotalPrice);
        invoice.DiscountAmount = invoice.SubTotal * invoice.DiscountRate;
        invoice.TaxAmount = (invoice.SubTotal - invoice.DiscountAmount) * invoice.TaxRate;
        invoice.TotalAmount = invoice.SubTotal - invoice.DiscountAmount + invoice.TaxAmount;

        if (!invoice.Items.Any())
        {
            invoice.Status = InvoiceStatus.Refunded;
        }

        invoice.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Invoices.UpdateAsync(invoice);
        await _unitOfWork.SaveChangesAsync();

        return invoice;
    }

    public async Task<decimal> GetDailySalesAsync(DateTime? date = null)
    {
        var targetDate = date ?? DateTime.UtcNow;
        var start = targetDate.Date;
        var end = start.AddDays(1).AddTicks(-1);
        return await _unitOfWork.Invoices.GetTotalSalesAsync(start, end);
    }

    public async Task<decimal> GetMonthlySalesAsync(int? year = null, int? month = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var start = new DateTime(targetYear, targetMonth, 1);
        var end = start.AddMonths(1).AddTicks(-1);
        return await _unitOfWork.Invoices.GetTotalSalesAsync(start, end);
    }

    private static string GenerateInvoiceNumber()
    {
        return $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }
}
