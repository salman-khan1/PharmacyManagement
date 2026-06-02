using System.ComponentModel.DataAnnotations;

namespace PharmacyManagement.Domain.Models;

public class InvoiceItem : BaseEntity
{
    [Required]
    public Guid InvoiceId { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    [Required]
    public Guid MedicineId { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string MedicineName { get; set; } = string.Empty;

    [StringLength(100)]
    public string BatchNumber { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal TotalPrice { get; set; }
}
