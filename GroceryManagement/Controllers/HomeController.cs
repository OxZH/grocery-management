using Microsoft.AspNetCore.Mvc;

namespace GroceryManagement.Controllers;

public class HomeController(DB db, IWebHostEnvironment en) : Controller
{
    public IActionResult Index()
    {
        var Inv = db.Inventories;
        return View(Inv);
    }
}
