using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace GroceryManagement.Models;
//using Microsoft.AspNetCore.Http;

#nullable disable warnings
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
    [Required(ErrorMessage = "Name is required.")]
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

// Task Management ViewModels

public class StaffAvailability
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string AttendanceStatus { get; set; } // ATTEND, ABSENT, LEAVE
    public int CurrentTaskCount { get; set; }
    public string? AuthorizationLvl { get; set; }
}

public class TaskAssignVM
{
    public string? TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string Priority { get; set; } = "MEDIUM";
    public string Category { get; set; } = "OTHER";
    public string? Description { get; set; }
    public int? DefaultDuration { get; set; }
    public DateOnly AssignedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public string RecurrencePattern { get; set; } = "NONE";
    public string? SelectedStaffId { get; set; }

    public List<StaffAvailability> AvailableStaff { get; set; } = [];
    public SelectList? TaskTemplates { get; set; }
}

public class TaskAllocationVM
{
    public string AllocationId { get; set; }
    public string TaskId { get; set; }
    public string TaskName { get; set; }
    public string Category { get; set; }
    public string Priority { get; set; }
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public string Status { get; set; }
    public DateOnly AssignedDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? EstimatedDuration { get; set; }
    public string? Notes { get; set; }
    public bool IsAbsent { get; set; } // Flag if staff is absent
    public string? AttendanceStatus { get; set; }
}

public class MonitorDashboardVM
{
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public List<TaskAllocationVM> PendingTasks { get; set; } = [];
    public List<TaskAllocationVM> InProgressTasks { get; set; } = [];
    public List<TaskAllocationVM> CompletedTasks { get; set; } = [];
    public List<TaskAllocationVM> AbsentStaffTasks { get; set; } = [];
    public int TotalTasks => PendingTasks.Count + InProgressTasks.Count + CompletedTasks.Count;
}

public class MyTasksVM
{
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public DateOnly Today { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public List<TaskAllocationVM> HighPriorityTasks { get; set; } = [];
    public List<TaskAllocationVM> MediumPriorityTasks { get; set; } = [];
    public List<TaskAllocationVM> LowPriorityTasks { get; set; } = [];
}

public class TaskTemplateVM
{
    public string? Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public string Category { get; set; } = "OTHER";

    public int? DefaultDuration { get; set; }

    [Required]
    public string Priority { get; set; } = "MEDIUM";
}

public class ReassignTaskVM
{
    public string AllocationId { get; set; }
    public string TaskName { get; set; }
    public string CurrentStaffId { get; set; }
    public string CurrentStaffName { get; set; }
    public DateOnly AssignedDate { get; set; }
    public List<StaffAvailability> AvailableStaff { get; set; } = [];
    public string? SelectedNewStaffId { get; set; }
}

// Roster Template Scheduling ViewModels

public class RosterTemplateFormVM
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Template name is required")]
    [MaxLength(100)]
    public string TemplateName { get; set; }

    public List<TemplateAllocationVM> Allocations { get; set; } = new();
}

public class TemplateAllocationVM
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Please select a staff member")]
    public string StaffId { get; set; }

    public string? StaffName { get; set; }

    [Required(ErrorMessage = "Task name is required")]
    [MaxLength(100)]
    public string TaskName { get; set; }

    public int SortOrder { get; set; }
}

public class CalendarMonthVM
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; }
    public List<CalendarDayVM> Days { get; set; } = new();
    public List<string> DayHeaders { get; set; } = new() { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
}

public class CalendarDayVM
{
    public DateOnly Date { get; set; }
    public int DayNumber { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool HasSchedule { get; set; }
    public string? TemplateName { get; set; }
    public string Status { get; set; } = "EMPTY"; // EMPTY, OK, WARNING, ACKNOWLEDGED
}

public class ApplyTemplateVM
{
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Please select a template")]
    public string? SelectedTemplateId { get; set; }

    public SelectList? Templates { get; set; }
    public List<TemplatePreviewVM> Preview { get; set; } = new();
}

public class TemplatePreviewVM
{
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public string TaskName { get; set; }
    public string AttendanceStatus { get; set; } = "UNKNOWN"; // ATTEND, ABSENT, LEAVE, UNKNOWN
}

public class DayDetailsVM
{
    public DateOnly Date { get; set; }
    public string TemplateName { get; set; }
    public bool HasUnavailableStaff { get; set; }
    public bool IsAcknowledged { get; set; }
    public List<DayAllocationVM> Allocations { get; set; } = new();
    public List<Staff> AvailableStaffForReassignment { get; set; } = new();
}

public class DayAllocationVM
{
    public string AllocationId { get; set; }
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public string TaskName { get; set; }
    public string AttendanceStatus { get; set; } = "UNKNOWN";
    public bool IsUnavailable { get; set; }
    public string Status { get; set; } // PENDING, IN_PROGRESS, COMPLETED
    public DateTime? StartTime { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Notes { get; set; }
}

public class MyScheduleVM
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; }
    public List<MyCalendarDayVM> Days { get; set; } = new();
    public string StaffId { get; set; }
    public string StaffName { get; set; }
}

public class MyCalendarDayVM
{
    public DateOnly Date { get; set; }
    public int DayNumber { get; set; }
    public bool IsCurrentMonth { get; set; }
    public bool HasAssignment { get; set; }
    public string? TaskName { get; set; }
}

public class MyDayTaskVM
{
    public DateOnly Date { get; set; }
    public string AllocationId { get; set; }
    public string TaskName { get; set; }
    public string Status { get; set; } // PENDING, IN_PROGRESS, COMPLETED
    public DateTime? StartTime { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Notes { get; set; }
    public List<TeammateVM> Teammates { get; set; } = new();
}

public class TeammateVM
{
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public string AttendanceStatus { get; set; } = "UNKNOWN";
}

public class DayManagementVM
{
    public DateOnly Date { get; set; }
    public bool HasSchedule { get; set; }
    
    // For Apply Template Tab
    public List<RosterTemplate> Templates { get; set; } = new();
    
    // For Attendance Tab
    public List<StaffAttendanceVM> StaffList { get; set; } = new();
}

public class StaffAttendanceVM
{
    public string StaffId { get; set; }
    public string StaffName { get; set; }
    public string? PhotoURL { get; set; }
    public string AttendanceStatus { get; set; } = "UNKNOWN";
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

// reports n stuff
public class DateRangeVM
{
    [DisplayName("Start Date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; }
    [DisplayName("End Date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; }
}