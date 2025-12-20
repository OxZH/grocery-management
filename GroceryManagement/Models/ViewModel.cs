using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace GroceryManagement.Models;
//using Microsoft.AspNetCore.Http;

public class InventoryInsertVM
{

    [MaxLength(6), Required(ErrorMessage = "Product ID is required"),
        RegularExpression(@"P\d{5}", ErrorMessage = "Format must be 'P' followed by 4 digits (e.g. P0001)")]
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
    [Required(ErrorMessage = "Invenotry Id is required")]
    [StringLength(9)]
    public string Id { get; set; }
    [MaxLength(6), Required(ErrorMessage = "Product ID is required"),
        RegularExpression(@"P\d{5}", ErrorMessage = "Format must be 'P' followed by 4 digits (e.g. P0001)")]
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
    //Holds the new photo file
    public IFormFile? Photo { get; set; }
    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int WareHouseQty { get; set; }

    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int StoreFrontQty { get; set; }
}
public class ProductMoveVM
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? PhotoURL { get; set; }
    public int WarehouseQty { get; set; }
    public int StoreFrontQty { get; set; }

    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int QtyToMove { get; set; }
}

public class RegisterVM
{
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z\s\.\'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }

    [StringLength(100)]
    [EmailAddress]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }

    [MaxLength(11)]
    [RegularExpression(@"^01[0-9]-?[0-9]{7,8}$", ErrorMessage = "Invalid Phone Number format.")]
    public string PhoneNum { get; set; }

    public IFormFile Photo { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Salary must be greater than 0 and less than RM 100,000")]
    public decimal Salary { get; set; }

    [RegularExpression(@"^(CLEANING|CASHIER|INVENTORY)$", ErrorMessage = "Role only can be one of the below: CLEANING, CASHIER, INVENTORY")]
    [Display(Name = "Job Role")]
    public string AuthorizationLvl { get; set; }

    [Display(Name = "User Role")]
    public string Role { get; set; } = "Staff";
}


public class LoginVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }

}


public class UpdatePasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [DisplayName("Current Password")]
    public string? Current { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [DisplayName("New Password")]
    public string? New { get; set; }

    [StringLength(100, MinimumLength = 8)]
    [Compare("New", ErrorMessage = "The new password and confirmation password do not match.")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string? Confirm { get; set; }
    public string? Token { get; set; }

}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }
    [MaxLength(11)]
    [RegularExpression(@"^01[0-9]-?[0-9]{7,8}$", ErrorMessage = "Invalid Phone Number format.")]
    public string? PhoneNum { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

public class UserUpdateVM
{
    public string Id { get; set; }

    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z\s\.\'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }

    //public string? Email { get; set; }

    [MaxLength(11)]
    [RegularExpression(@"^01[0-9]-?[0-9]{7,8}$", ErrorMessage = "Invalid Phone Number format.")]
    public string PhoneNum { get; set; }

    public IFormFile? Photo { get; set; }

    public string? ExistingPhotoURL { get; set; }

    [Range(0.01, 100000, ErrorMessage = "Salary must be greater than 0")]
    public decimal? Salary { get; set; }

    [RegularExpression(@"^(CLEANING|CASHIER|INVENTORY)$", ErrorMessage = "Role only can be one of the below: CLEANING, CASHIER, INVENTORY")]
    [Display(Name = "Job Role")]
    public string? AuthorizationLvl { get; set; }

    public string? ManagerId { get; set; }
    public SelectList? ManagerList { get; set; }

    public string? Role { get; set; }
}