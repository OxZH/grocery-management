global using GroceryManagement.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\GroceryManagementDB.mdf;
");

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapDefaultControllerRoute();

// Ensure database exists and seed sample orders for development/testing
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    try
    {
        db.Database.EnsureCreated();

        if (!db.Set<Checkout>().Any())
        {
            var now = DateTime.Now;
            var sample = new List<Checkout>
            {
                new Checkout { Id = "O0001", CustomerId = "CUST1", InventoryId = "INV00001A", Total = 29.99m, Date = now.AddDays(-2), Status = "PENDING", StatusUpdateDate = now.AddDays(-2), PaymentMethod = "CASH" },
                new Checkout { Id = "O0002", CustomerId = "CUST2", InventoryId = "INV00002A", Total = 54.75m, Date = now.AddDays(-1), Status = "PICKING", StatusUpdateDate = now.AddDays(-1), PaymentMethod = "E-WALLET" },
                new Checkout { Id = "O0003", CustomerId = "CUST3", InventoryId = "INV00003A", Total = 12.50m, Date = now, Status = "READY", StatusUpdateDate = now, PaymentMethod = "BANK PAYMENT" }
            };
            db.AddRange(sample);
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error ensuring database is created or seeding sample data");
    }
}

app.Run();
