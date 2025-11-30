using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GroceryManagement.Models;

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Staff> Staffs { get; set; }
    public DbSet<Manager> Managers { get; set; }

}

public class User
{
    [Key, MaxLength(4)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [MaxLength(100)]
    public string Email { get; set; }
    [MaxLength(100)]
    public string Password { get; set; }
    [MaxLength(11)]
    public string PhoneNum { get; set; }
    public string Role => GetType().Name; // "Staff" or "Manager"

}

public class Staff:User
{
    [MaxLength(100)]
    public string? PhotoURL { get; set; }
    [Precision(10,2)]
    public decimal? Salary { get; set; }
    public string? AuthorizationLvl { get; set; }
    // FK
    public string? ManagerId { get; set; }

    //navigation
    public Manager Manager { get; set; }

    // relationship to Inventory
    // 1 Staff manages many Inventory items
    public List<Inventory> ManagedInventory { get; set; }


    // relationship to Orders
    // 1 Staff manages/processes many CustomerOrders
    public List<CustomerOrder> CustomerOrders { get; set; }


    // relationship to Tasks (via Allocation)
    // Staff has many Allocations (assignments to tasks)
    public List<Allocation> Allocations { get; set; }


    // relationship to Attendance
    // Staff has many AttendanceRecords (their own history)
    public List<AttendanceRecord> AttendanceRecords { get; set; }

}
public class Manager : User
{
    //navigation
    //if wan lazy loading then List change to ICollection
    public List<Staff> Staffs { get; set; } = [];
    public List<Expense> Expenses { get; set; } = [];
    public List<AttendanceRecord> AttendenceRecords { get; set; } = [];

}

public class CustomerOrder
{
    [Key, MaxLength(5)]
    public string Id { get; set; }
    //add remaining attributes

    //nav

}

public class Allocation
{
    [Key,MaxLength(5)]
    public string Id { get; set; }
    //add remaining attributes

    //nav
}

public class AttendanceRecord
{
    [Key, MaxLength(5)]
    public string Id { get; set; }
    //add remaining attributes

    //nav
    public Manager Manager { get; set; }
}

public class Expense
{
    [Key, MaxLength(5)]
    public string Id { get; set; }
    //add remaining attributes

    //nav
    public Manager Manager { get; set; }
}

public class Product
{
    [Key, MaxLength(5)]
    public string Id { get; set; }
    [MaxLength(100)]
    public string Name { get; set; }
    [Precision(6, 2)]
    public decimal Price { get; set; }
    [MaxLength(100)]
    public string PhotoURL { get; set; }
    public string Category { get; set; }
    public string SupplierId { get; set; }
    //Navigation
    public List<Inventory> Inventories { get; set; } = [];

}
public class Inventory
{
    [Key, MaxLength(10)]
    public string Id { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public int WareHouseQty { get; set; }
    public int StoreFrontQty { get; set; }
    //FK
    public string ProductId { get; set; }
    //Navigation
    public Product Product { get; set; }

}
