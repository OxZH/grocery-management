using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GroceryManagement.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<Supplier> Supplier { get; set; }
    public DbSet<ProcurementRecord> ProcurementRecords { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    //public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<Expense> Expenses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Manager> Managers { get; set; }
    public DbSet<AttendanceRecords> AttendanceRecords { get; set; }
    public DbSet<TaskType> TaskTypes { get; set; }
    public DbSet<RosterTemplate> RosterTemplates { get; set; }
    public DbSet<TemplateAllocation> TemplateAllocations { get; set; }
    public DbSet<DaySchedule> DaySchedules { get; set; }
    public DbSet<Allocation> Allocations { get; set; }
    public DbSet<LeaveRequest> LeaveRequests { get; set; }
    public DbSet<Checkout> Checkout { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure cascade delete behavior to prevent cycles
        modelBuilder.Entity<TemplateAllocation>()
            .HasOne(ta => ta.Staff)
            .WithMany(s => s.TemplateAllocations)
            .HasForeignKey(ta => ta.StaffId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete from Staff

        modelBuilder.Entity<TemplateAllocation>()
            .HasOne(ta => ta.Template)
            .WithMany(t => t.Allocations)
            .HasForeignKey(ta => ta.TemplateId)
            .OnDelete(DeleteBehavior.Cascade); // Allow cascade delete from Template

        modelBuilder.Entity<DaySchedule>()
            .HasOne(ds => ds.Template)
            .WithMany(t => t.DaySchedules)
            .HasForeignKey(ds => ds.TemplateId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete from Template

        modelBuilder.Entity<DaySchedule>()
            .HasOne(ds => ds.Manager)
            .WithMany(m => m.DaySchedules)
            .HasForeignKey(ds => ds.AppliedBy)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete from Manager

        modelBuilder.Entity<Allocation>()
            .HasOne(a => a.Staff)
            .WithMany(s => s.Allocations)
            .HasForeignKey(a => a.StaffId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete from Staff
    }
}

public class User
{
    [Key, MaxLength(4)]
    [RegularExpression(@"^[a-zA-Z][0-9]{2,3}$", ErrorMessage = "ID must be 1 letter followed by 2-3 digits (e.g., S01)")]
    public string Id { get; set; }
    [MaxLength(100), Required(ErrorMessage = "Name is required.")]
    [RegularExpression(@"^[a-zA-Z\s.'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }
    [MaxLength(100), Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string Email { get; set; }
    [MaxLength(100), Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; }
    [MaxLength(11), Required(ErrorMessage = "Phone Number is required.")]
    [RegularExpression(@"^01[0-9]-?[0-9]{7,8}$", ErrorMessage = "Invalid Phone Number format.")]
    public string PhoneNum { get; set; }
    public string Role => GetType().Name; // "Staff" or "Manager"

    public string? ResetToken { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime? ResetTokenExpiry { get; set; }

    public int LoginAttempts { get; set; } = 0;
    [DataType(DataType.DateTime)]
    public DateTime? Locked { get; set; }
}

public class Staff : User
{
    [MaxLength(100)]
    [RegularExpression(@".+\.(jpg|jpeg|png)$", ErrorMessage = "Image must be .jpg, .jpeg, or .png")]
    public string? PhotoURL { get; set; }
    [Precision(7, 2)]
    [Range(0.01, 99999.99, ErrorMessage = "Salary must be between 0 and 100,000")]
    public decimal? Salary { get; set; }
    [RegularExpression(@"^(CLEANING|CASHIER|INVENTORY)$", ErrorMessage = "Role only can be one of the below: CLEANING, CASHIER, INVENTORY")]
    public string? AuthorizationLvl { get; set; }
    // FK
    [RegularExpression(@"^[a-zA-Z][0-9]{2,3}$")]
    public string? ManagerId { get; set; }

    //navigation
    public Manager Manager { get; set; }
    public List<Inventory> ManagedInventory { get; set; }
    public List<Checkout> Checkout { get; set; }
    public List<Allocation> Allocations { get; set; }
    public List<AttendanceRecords> AttendanceRecords { get; set; }
    public List<LeaveRequest> LeaveRequests { get; set; }
    public List<TemplateAllocation> TemplateAllocations { get; set; } = [];

}
public class Manager : User
{
    //navigation
    //if wan lazy loading then List change to ICollection
    public List<Staff> Staffs { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<AttendanceRecords> AttendenceRecords { get; set; } = [];
    public List<LeaveRequest> LeaveRequests { get; set; }
    public List<RosterTemplate> RosterTemplates { get; set; } = [];
    public List<DaySchedule> DaySchedules { get; set; } = [];

}

public class Checkout
{
    [Key, MaxLength(6)]
    public string Id { get; set; }
    public string CustomerId { get; set; }
    public string InventoryId { get; set; }
    [Precision(7, 2)]
    public decimal Total { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime Date { get; set; }
    [RegularExpression("^(CONFIRMED|PENDING|FAILED)$", ErrorMessage = "Status must be one of: VONFIRMED, PENDING or FAILED")]
    [MaxLength(10)]
    public string Status { get; set; }
    [DataType(DataType.DateTime)]
    public DateTime StatusUpdateDate { get; set; }
    public string PaymentMethod { get; set; }
    //add remaining attributes

    //nav
    public Staff Staff { get; set; }
    public List<Inventory> Inventories { get; set; } = [];
}

public class TaskType
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class RosterTemplate
{
    [Key, MaxLength(8)]
    [RegularExpression(@"RST\d{5}", ErrorMessage = "Format must be 'RST' followed by 5 digits (e.g. RST00001)")]
    public string Id { get; set; }

    [Required, MaxLength(100)]
    public string TemplateName { get; set; }

    [Required, MaxLength(4)]
    public string ManagerId { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Manager Manager { get; set; }
    public List<TemplateAllocation> Allocations { get; set; } = [];
    public List<DaySchedule> DaySchedules { get; set; } = [];
}

public class TemplateAllocation
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(8)]
    public string TemplateId { get; set; }

    [Required, MaxLength(4)]
    public string StaffId { get; set; }

    [Required, MaxLength(100)]
    public string TaskName { get; set; }

    public int SortOrder { get; set; }

    // Navigation
    public RosterTemplate Template { get; set; }
    public Staff Staff { get; set; }
}

public class DaySchedule
{
    [Key]
    public int Id { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly ScheduleDate { get; set; }

    [Required, MaxLength(8)]
    public string TemplateId { get; set; }

    [Required, MaxLength(4)]
    public string AppliedBy { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime AppliedAt { get; set; } = DateTime.Now;

    public bool HasUnavailableStaff { get; set; } = false;

    public bool IsAcknowledged { get; set; } = false;

    // Navigation
    public RosterTemplate Template { get; set; }
    public Manager Manager { get; set; }
}

public class Allocation
{
    [Key, MaxLength(10)]
    [RegularExpression(@"ALC\d{7}", ErrorMessage = "Format must be 'ALC' followed by 7 digits (e.g. ALC0000001)")]
    public string Id { get; set; }

    [MaxLength(8)]
    public string? TemplateId { get; set; }

    [Required, MaxLength(4)]
    public string StaffId { get; set; }

    [Required, MaxLength(100)]
    public string TaskName { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly AssignedDate { get; set; }

    // Navigation
    public Staff Staff { get; set; }
}

public class AttendanceRecords
{
    [Key, MaxLength(10)]
    [RegularExpression(@"ATT\d{5}", ErrorMessage = "Format must be 'ATT', 5 digits (e.g. ATT00001)")]
    public string Id { get; set; }
    public string StaffId { get; set; }
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; }
    [DataType(DataType.Time)]
    public TimeOnly? CheckInTime { get; set; }
    [DataType(DataType.Time)]
    public TimeOnly? CheckOutTime { get; set; }

    [RegularExpression("^(ATTEND|ABSENT|LEAVE)$", ErrorMessage = "Status must be one of: ATTEND, ABSENT, or LEAVE")]
    [MaxLength(10)]
    public string Status { get; set; }

    public Staff Staff { get; set; }
    public Manager Manager { get; set; }
}

public class LeaveRequest
{
    [Key, MaxLength(8)]
    [RegularExpression(@"LR\d{5}", ErrorMessage = "Format must be 'LR' followed by 5 digits (e.g. LR00001)")]
    public string Id { get; set; }

    [Required, RegularExpression(@"^[a-zA-Z][0-9]{2,3}$", ErrorMessage = "Staff ID must be letter + 2-3 digits")]
    public string StaffId { get; set; }

    [Required, DataType(DataType.Date)]
    public DateOnly LeaveDate { get; set; }

    [Required, MaxLength(10)]
    [RegularExpression("^(ADVANCE|MC)$", ErrorMessage = "Type must be ADVANCE or MC")]
    public string Type { get; set; }

    [Required, MaxLength(10)]
    [RegularExpression("^(PENDING|APPROVED|REJECTED)$", ErrorMessage = "Status must be PENDING, APPROVED, or REJECTED")]
    public string Status { get; set; } = "PENDING";

    [MaxLength(300)]
    public string? Reason { get; set; }

    [MaxLength(260)]
    public string? AttachmentPath { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime SubmittedAt { get; set; } = DateTime.Now;

    [RegularExpression(@"^[a-zA-Z][0-9]{2,3}$")]
    public string? ApprovedBy { get; set; }

    [DataType(DataType.DateTime)]
    public DateTime? ApprovedAt { get; set; }

    // Navigation
    public Staff? Staff { get; set; }
    public Manager? Manager { get; set; }
}
public class Expense
{
    [Key, MaxLength(6)]
    public string Id { get; set; }
    // attributes
    [Required]
    [MaxLength(50)]
    public string Type { get; set; }

    [MaxLength(500)]
    public string Details { get; set; }

    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Precision(10, 2)]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be positive")]
    public decimal Amount { get; set; }
    // added optional staffid
    public string? StaffId { get; set; }
    public string ManagerId { get; set; }
    //nav / FK
    public Manager Manager { get; set; }
    // added Fk to staff
    public Staff? Staff { get; set; }
}

public class Product
{
    [Key, MaxLength(6), Required(ErrorMessage = "Product ID is required"),
        RegularExpression(@"P\d{5}", ErrorMessage = "Format must be 'P' followed by 4 digits (e.g. P0001)")]
    public string Id { get; set; }

    [Required(ErrorMessage = "Product Name is required"),
        MaxLength(100, ErrorMessage = "Product Name cannot exceed 100 characters")]
    public string Name { get; set; }

    [Required(ErrorMessage = "SellPrice is required"),
        Range(0.01, 10000.00, ErrorMessage = "SellPrice must be greater than 0"),
        Precision(7, 2)]
    public decimal SellPrice { get; set; }

    [RegularExpression(@".+\.(jpg|jpeg|png)$", ErrorMessage = "Image must be .jpg, .jpeg, or .png")]
    public string? PhotoURL { get; set; }

    [Required(ErrorMessage = "Category is required"), MaxLength(20)]
    public string Category { get; set; }

    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int WareHouseQty { get; set; }

    [Range(0, 9999, ErrorMessage = "Quantity cannot be negative")]
    public int StoreFrontQty { get; set; }

    //Navigation
    public List<Inventory> Inventories { get; set; } = [];


}

public class Inventory
{
    [Key, MaxLength(9), Required(ErrorMessage = "Id is required"),
        RegularExpression(@"INV\d{5}[A-Z]", ErrorMessage = "Format must be 'INV', 5 digits, and a letter (e.g. INV00001A)")]
    public string Id { get; set; }

    [Required(ErrorMessage = "Expiry Date is required")]
    [DataType(DataType.Date)]
    public DateOnly ExpiryDate { get; set; }

    //FK
    [Required(ErrorMessage = "Please select a Product")]
    public string ProductId { get; set; }
    [Required(ErrorMessage = "Staff Id is required")]
    public string StaffId { get; set; }

    [Required(ErrorMessage = "Supplier ID is required"),
   RegularExpression(@"SUP\d{3}", ErrorMessage = "Format must be 'SUP' followed by 3 digits (e.g. SUP001)")]
    public string SupplierId { get; set; }

    [RegularExpression("^(AVAILABLE|SOLD|DISPOSED)$", ErrorMessage = "Status must be one of: Available, Sold, or Disposed")]
    [MaxLength(10)]
    public string Status { get; set; }
    public string? CheckoutId { get; set; }

    //Navigation
    public Product Product { get; set; }
    public Staff Staff { get; set; }
    public Checkout Checkout { get; set; }
    public Supplier Supplier { get; set; }
}

public class Supplier
{
    [Key, MaxLength(6)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(12)]
    public string SupplierType { get; set; }

    [MaxLength(250)]
    public string Address { get; set; }

    [MinLength(11), MaxLength(12)]
    public string ContactNo { get; set; }
}

public class ProcurementRecord
{
    [Key, MaxLength(10)]
    public string Id { get; set; }

    public int Quantity { get; set; }

    [Precision(6, 2)]
    [DataType(DataType.Currency)]
    public Decimal TotalPrice { get; set; }

    // quick access
    [MaxLength(10)]
    public string Status { get; set; }

    [MaxLength(6)]
    public string PaymentStatus { get; set; }

    // automated
    [DataType(DataType.DateTime)]
    public DateTime ProcurementDateTime { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StatusUpdateDateTime { get; set; }

    [DataType(DataType.Url)]
    [MaxLength(100)]
    public string? DeliveryProofPhotoLink { get; set; }

    // FK
    [MaxLength(6)]
    public string ProductId { get; set; }

    [MaxLength(6)]
    public string SupplierId { get; set; }

    // Navigation
    public Product Product { get; set; }
    public Supplier Supplier { get; set; }
}
/*public class Suppliers
{
    [Key, MaxLength(6)]
    public string Id { get; set; }

    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(11)]
    public string SupplierType { get; set; }

    [MaxLength(250)]
    public string Address { get; set; }

    [MinLength(11), MaxLength(12)]
    public string ContactNo { get; set; }
}
*/
public class ProcurementRecords
{
    [Key, MaxLength(10)]
    public string Id { get; set; }

    public int Quantity { get; set; }

    [Precision(6, 2)]
    [DataType(DataType.Currency)]
    public Decimal TotalPrice { get; set; }

    // quick access
    [MaxLength(10)]
    public string Status { get; set; }

    [MaxLength(6)]
    [RegularExpression("^(Paid|Unpaid)$", ErrorMessage = "Invalid payment status")]
    public string PaymentStatus { get; set; }

    // automated
    [DataType(DataType.DateTime)]
    public DateTime ProcurementDateTime { get; set; }

    [DataType(DataType.Date)]
    public DateTime? StatusUpdateDateTime { get; set; }

    [DataType(DataType.Url)]
    [MaxLength(100)]
    public string? DeliveryProofPhotoLink { get; set; }

    // FK
    [MaxLength(6)]
    public string ProductId { get; set; }

    [MaxLength(6)]
    public string SupplierId { get; set; }

    // Navigation
    public Product Product { get; set; } // TODO: deal with this later
    public Supplier Supplier { get; set; }
}