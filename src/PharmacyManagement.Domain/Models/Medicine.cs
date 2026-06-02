using System.ComponentModel.DataAnnotations;

namespace PharmacyManagement.Domain.Models;

public class Medicine : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string Barcode { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string MedicineName { get; set; } = string.Empty;

    [StringLength(200)]
    public string GenericName { get; set; } = string.Empty;

    [StringLength(200)]
    public string BrandName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [StringLength(100)]
    public string BatchNumber { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal PurchasePrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal SellingPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int MinimumQuantity { get; set; } = 10;

    [StringLength(50)]
    public string RackNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string ShelfNumber { get; set; } = string.Empty;

    [StringLength(200)]
    public string ExactLocation { get; set; } = string.Empty;

    public DateTime? ExpiryDate { get; set; }

    public DateTime? ManufacturingDate { get; set; }

    [StringLength(200)]
    public string Supplier { get; set; } = string.Empty;

    public bool PrescriptionRequired { get; set; } = false;

    [StringLength(500)]
    public string ImagePath { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    public virtual ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
}
