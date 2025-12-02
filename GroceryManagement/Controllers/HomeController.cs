using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GroceryManagement.Controllers;

public class HomeController(DB db, IWebHostEnvironment en) : Controller
{
    public IActionResult Index()
    {
        var Inv = db.Inventories;
        return View(Inv);
    }
    public IActionResult Insert()
    {
        ViewBag.ProductList = new SelectList(db.Products, "Id", "Name");
        return View();
    }

    // POST: Home/Insert
    [HttpPost]
    public IActionResult Insert(InventoryInsertVM vm)
    {

        if (ModelState.IsValid("Id") &&
            db.Products.Any(s => s.Id == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated ID. ");
        }
        if (ModelState.IsValid("ProductId") &&
            !db.Products.Any(p => p.Id == vm.ProductId))
        {
            ModelState.AddModelError("ProductId", "Invalid PRODUCT. ");
        }
        if (ModelState.IsValid("ExpiryDate"))
        {
            var a = DateTime.Today.AddDays(-30).ToDateOnly();
            if (vm.ExpiryDate < a)
            {
                ModelState.AddModelError("Date", "Date out of range.");
            }
        }
        if (ModelState.IsValid)
        {
            db.Inventories.Add(new()
            {
                Id = vm.Id.Trim().ToUpper(),
                ProductId = vm.ProductId,
                ExpiryDate = vm.ExpiryDate,
                WareHouseQty = vm.WareHouseQty,
                StoreFrontQty = vm.StoreFrontQty,
            });
            db.SaveChanges();
            TempData["Info"] = "Record inserted.";
            return RedirectToAction("Index");
        }
        ViewBag.ProgramList = new SelectList(db.Inventories, "Id", "Name");
        return View();
    }

    public IActionResult TestDBUsers()
    {
        var users = db.Users;
        return View(users);
    }


}
