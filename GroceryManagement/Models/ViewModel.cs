using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace GroceryManagement.Models;

#nullable disable warnings

public class InventoryInsertVM
{
    [Required(ErrorMessage = "Batch ID is required")]
    [StringLength(10)]
    public string Id { get; set; }

    [Required(ErrorMessage = "Please select a product")]
    public string ProductId { get; set; }

    [Required(ErrorMessage = "Expiry Date is required")]
    [DataType(DataType.Date)]
    public DateOnly ExpiryDate { get; set; }

    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int WareHouseQty { get; set; }
    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int StoreFrontQty { get; set; }
}

public class SupplierVM
{
    // format: SUP001
    [StringLength(6)]
    [RegularExpression(@"SUP\d{3}", ErrorMessage = "Invalid {0}")]
    public string? Id { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    [StringLength(100)]
    public string Name { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    [DisplayName("Supplier Type")]
    [StringLength(11)]
    [RegularExpression("^(Distributor|Wholesale|White-Label)$", ErrorMessage = "Invalid {0}")]
    public string SupplierType { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    [StringLength(250)]
    public string Address { get; set; }

    [Required(ErrorMessage = "This field is required.")]
    [DisplayName("Contact Number")]
    [StringLength(12)]
    [RegularExpression(@"01\d-(\d){7,8}", ErrorMessage = "Invalid {0}")]
    public string ContactNo { get; set; }
}

public class SupplierTagVM
{
    [Required(ErrorMessage = "Tag Name is required")]
    [StringLength(50)]
    public string Name { get; set; }
}

public class ProcurementRecordVM
{
    [Key, StringLength(10)]
    [RegularExpression(@"^PR\d{6}$", ErrorMessage = "Format must be 'PR' followed by 6 digits (e.g. PR000001)")]
    public string? Id { get; set; }

    [Range(1, 9999, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    // FK
    [Required(ErrorMessage = "Product ID is required")]
    [StringLength(6)]
    [RegularExpression(@"P\d{5}")]
    public string ProductId { get; set; }

    [Required(ErrorMessage = "Supplier ID is required")]
    [StringLength(6)]
    [RegularExpression(@"SUP\d{3}")]
    public string SupplierId { get; set; }

    [StringLength(10)]
    [RegularExpression("^(Ordered|Received|Cancelled)$")]
    public string? Status { get; set; }

    public Decimal TotalPrice { get; set; }

    // other stuff
    public IFormFile? ProofPhoto { get; set; }
}