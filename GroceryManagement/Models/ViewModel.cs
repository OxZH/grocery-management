using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
    public string Role { get; set; }  = "Staff";
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
    public string Id {get; set; }

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
/*public class ResetPasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; }
    public string Token { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; }

}*/