using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace GroceryManagement.Models;

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
    [Required(ErrorMessage = "Please select a staff")]
    [RegularExpression(@"^[a-zA-Z][0-9]{2,3}$", ErrorMessage = "Staff ID must be letter + 2-3 digits")]
    public string StaffId { get; set; } = string.Empty;

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