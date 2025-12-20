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
