global using GroceryManagement.Models;
global using GroceryManagement;
using GroceryManagement.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\GroceryManagementDB.mdf;
");
builder.Services.AddScoped<Helper>();
builder.Services.AddAuthentication().AddCookie();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<InventoryHub>("/InventoryHub");
app.MapDefaultControllerRoute();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DB>();
    var hp = scope.ServiceProvider.GetRequiredService<Helper>();
    DataSeeder.SeedManager(db, hp);
}

app.Run();
