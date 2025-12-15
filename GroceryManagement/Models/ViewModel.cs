using Microsoft.AspNetCore.Mvc;
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
    [Remote("CheckId", "Supplier", ErrorMessage = "Duplicated {0}.")]
    public string Id { get; set; }

    [StringLength(100)]
    public string Name { get; set; }

    [DisplayName("Supplier Type")]
    [StringLength(11)]
    [RegularExpression("^(Distributor|Wholesale|White-Label)$", ErrorMessage = "Invalid {0}")]
    public string SupplierType { get; set; }

    [StringLength(250)]
    public string Address { get; set; }

    [DisplayName("Contact Number")]
    [StringLength(12)]
    [RegularExpression(@"01\d-(\d){7,8}", ErrorMessage = "Invalid {0}")]
    public string ContactNo { get; set; }
}