using PharmacyManagement.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace PharmacyManagement.Domain.Models;

public class StockTransaction : BaseEntity
{
    [Required]
    public Guid MedicineId { get; set; }

    public virtual Medicine Medicine { get; set; } = null!;

    public StockTransactionType TransactionType { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    [StringLength(200)]
    public string ReferenceNumber { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    [StringLength(200)]
    public string Supplier { get; set; } = string.Empty;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}
