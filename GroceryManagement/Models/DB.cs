using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace GroceryManagement.Models;

public class DB(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }
    public DbSet<Inventory> Inventories { get; set; }
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
