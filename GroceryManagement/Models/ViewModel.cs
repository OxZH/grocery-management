using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;
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

public class LeaveRequestFormVM
{
    [Required(ErrorMessage = "Leave date is required")]
    [DataType(DataType.Date)]
    public DateOnly LeaveDate { get; set; }

    [Required(ErrorMessage = "Type is required")]
    public string Type { get; set; } = "ADVANCE";

    [MaxLength(300)]
    public string? Reason { get; set; }
}

public class LeaveApplyVM
{
    public LeaveRequestFormVM Form { get; set; } = new();
    public List<LeaveRequest> Requests { get; set; } = [];
}

public class PayVM
{
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public string Role { get; set; } = "Staff";
    public decimal Salary { get; set; }
    [DisplayFormat(DataFormatString = "{0:F1}")]
    public double TotalHours { get; set; }
    public int MissingCheckouts { get; set; }
    public decimal TotalSalary { get; set; }
    public List<DailyPayDetailsVM> DailyDetails { get; set; } = new();
}

public class DailyPayDetailsVM
{
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; }
    public string CheckIn { get; set; }
    public string CheckOut { get; set; }
    [DisplayFormat(DataFormatString = "{0:F1}")]
    public double HoursWorked { get; set; }
    public decimal DailyPay { get; set; }
    public string Note { get; set; } 
}

public class RegisterVM
{
    [Required(ErrorMessage ="Name is required.")]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z\s\.\'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100)]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    [Remote("CheckEmail", "Account", ErrorMessage = "Duplicated {0}.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters and contain at least 1 uppercase letter, 1 number, and 1 symbol.")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Please confirm your password.")]
    [StringLength(100, MinimumLength = 8)]
    [Compare("Password")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
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
    [Required(ErrorMessage = "Email is required.")]
    [StringLength(100)]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100)]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }

}

public class UpdatePasswordVM
{
    [StringLength(100)]
    [EmailAddress]
    public string? Email { get; set; }
    [StringLength(100)]
    [DataType(DataType.Password)]
    [DisplayName("Current Password")]
    public string? Current { get; set; }

    [Required(ErrorMessage = "New Password is required.")]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$",
        ErrorMessage = "Password must be at least 8 characters, with 1 uppercase, 1 number, and 1 symbol.")]
    [DisplayName("New Password")]
    public string New { get; set; }

    [Required(ErrorMessage = "Please confirm new password.")]
    [Compare("New", ErrorMessage = "The new password and confirmation password do not match.")]
    [DataType(DataType.Password)]
    [DisplayName("Confirm Password")]
    public string Confirm { get; set; }
    public string? Token { get; set; }

}

public class UpdateProfileVM
{
    public string? Email { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z\s\.\'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }
    [Required(ErrorMessage = "Phone Number is required.")]
    [MaxLength(11)]
    [RegularExpression(@"^01[0-9]-?[0-9]{7,8}$", ErrorMessage = "Invalid Phone Number format.")]
    public string PhoneNum { get; set; }

    public string? PhotoURL { get; set; }

    public IFormFile? Photo { get; set; }
}

public class UserUpdateVM
{
    public string Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    [RegularExpression(@"^[a-zA-Z\s\.\'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Phone Number is required.")]
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
