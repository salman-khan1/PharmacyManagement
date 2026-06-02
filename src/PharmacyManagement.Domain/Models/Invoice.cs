using PharmacyManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PharmacyManagement.Domain.Models;

public class Invoice : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    [StringLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(500)]
    public string CustomerAddress { get; set; } = string.Empty;

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Paid;

    public decimal SubTotal { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TaxRate { get; set; } = 0.05m;

    public decimal DiscountAmount { get; set; }

    public decimal DiscountRate { get; set; }

    public decimal TotalAmount { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal ChangeAmount { get; set; }

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;

    public virtual ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
