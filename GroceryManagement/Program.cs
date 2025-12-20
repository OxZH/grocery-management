global using GroceryManagement.Models;
global using GroceryManagement;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<DB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\GroceryManagementDB.mdf;
");
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Helper>();

var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapDefaultControllerRoute();
app.Run();
