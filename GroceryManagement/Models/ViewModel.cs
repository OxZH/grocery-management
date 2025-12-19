using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
namespace GroceryManagement.Models;
//using Microsoft.AspNetCore.Http;

public class InventoryInsertVM
{

    [Required(ErrorMessage = "Please select a product")]
    public string ProductId { get; set; }

    [Required(ErrorMessage = "Expiry Date is required")]
    [DataType(DataType.Date)]
    public DateOnly ExpiryDate { get; set; }

    [Required(ErrorMessage = "Supplier ID is required"),
    RegularExpression(@"SUP\d{3}", ErrorMessage = "Format must be 'SUP' followed by 3 digits (e.g. SUP001)")]
    public string SupplierId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, 9999, ErrorMessage = "Quantity must be between 1 and 9999")]
    public int Qty { get; set; } 

}
public class InventoryUpdateVM
{
    [Required(ErrorMessage = "Inventory ID is required")]
    [StringLength(9)]
    public string Id { get; set; }

    [Required(ErrorMessage = "Please select a product")]
    public string ProductId { get; set; }

    [Required(ErrorMessage = "Expiry Date is required")]
    [DataType(DataType.Date)]
    public DateOnly ExpiryDate { get; set; }

    [Required(ErrorMessage = "Staff ID is required")]
    public string StaffId { get; set; }

    [Required(ErrorMessage = "Supplier ID is required"),
    RegularExpression(@"SUP\d{3}", ErrorMessage = "Format must be 'SUP' followed by 3 digits (e.g. SUP001)")]
    public string SupplierId { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [MaxLength(10)]
    public string Status { get; set; }

    [MaxLength(5)]
    public string? CheckoutId { get; set; }
}


public class ProductInsertVM
{
    [Required(ErrorMessage = "Product Name is required")]
    public string Name { get; set; }

    [Required(ErrorMessage = "SellPrice is required"),
        Range(0.01, 10000.00, ErrorMessage = "SellPrice must be greater than 0"),
        Precision(7, 2)]
    public Decimal SellPrice { get; set; }

    public IFormFile? PhotoURL { get; set; }

    [Required(ErrorMessage = "Category is required"), MaxLength(20)]
    public string Category { get; set; }
}
public class ProductUpdateVM
{
    [Required(ErrorMessage = "Product Id is required")]
    [StringLength(9)]
    public string Id { get; set; }

    [Required(ErrorMessage = "Product Name is required")]
    [MaxLength(100, ErrorMessage = "Product Name cannot exceed 100 characters")]
    public string Name { get; set; }

    [Required(ErrorMessage = "SellPrice is required")]
    [Range(0.01, 10000.00, ErrorMessage = "SellPrice must be greater than 0")]
    public decimal SellPrice { get; set; }

    [Required(ErrorMessage = "Category is required")]
    [MaxLength(20)]
    public string Category { get; set; }

    //Holds the current photo filename
    public string? PhotoURL { get; set; }
    //Holds the NEW photo file
    public IFormFile? Photo { get; set; }
    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int WareHouseQty { get; set; }

    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int StoreFrontQty { get; set; }
}