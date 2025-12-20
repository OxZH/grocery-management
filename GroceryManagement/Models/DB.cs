using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GroceryManagement.Models;

#nullable disable warnings

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Manager> Managers { get; set; }
    public DbSet<AttendanceRecords> AttendanceRecords { get; set; }
    public DbSet<Expense> Expenses { get; set; }

}

public class User
{
    [Key, MaxLength(4)]
    [RegularExpression(@"^[a-zA-Z][0-9]{2,3}$", ErrorMessage = "ID must be 1 letter followed by 2-3 digits (e.g., S01)")]
    public string Id { get; set; }
    [MaxLength(100), Required(ErrorMessage = "Name is required.")]
    [RegularExpression(@"^[a-zA-Z\s\.\'-]+$", ErrorMessage = "Name can only contain letters, spaces, and .'-")]
    public string Name { get; set; }
    [MaxLength(100), Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
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
    //public List<Allocation> Allocations { get; set; }
    public List<AttendanceRecords> AttendanceRecords { get; set; }

}
public class Manager : User
{
    //navigation
    //if wan lazy loading then List change to ICollection
    public List<Staff> Staffs { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<AttendanceRecords> AttendenceRecords { get; set; } = [];

}


public class Checkout
{
    [Key, MaxLength(5)]
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

/*public class Allocation
{
    [Key, MaxLength(5)]
    public string Id { get; set; }
    //add remaining attributes

    //nav
    public Staff Staff { get; set; }
}
*/

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

    [RegularExpression("^(ATTEND|ABSENT|LATE|LEAVE)$", ErrorMessage = "Status must be one of: ATTEND, ABSENT, LATE, or LEAVE")]
    [MaxLength(10)]
    public string Status { get; set; }

    public Staff Staff { get; set; }
    public Manager Manager { get; set; }
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

}
