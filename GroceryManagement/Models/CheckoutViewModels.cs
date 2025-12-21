using System.ComponentModel.DataAnnotations;

namespace GroceryManagement.Models;

public class CheckoutItemInputVM
{
    [Required]
    [MaxLength(6)]
    public string ProductId { get; set; }

    [Range(1, 9999)]
    public int Quantity { get; set; } = 1;
}

public class CheckoutCreateVM
{
    [Required]
    public string CustomerId { get; set; }

    [MaxLength(20)]
    public string? PaymentMethod { get; set; }

    [MaxLength(10)]
    public string? Status { get; set; }

    public List<CheckoutItemInputVM> Items { get; set; } = new() { new CheckoutItemInputVM() };
}
