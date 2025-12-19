using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GroceryManagement.Controllers;

public class ProductController(DB db, Helper hp) : Controller
{
    public IActionResult Index()
    {
        var m = db.Products.ToList();
        return View(m);
    }


    public IActionResult Insert()
    {
        // filter duplicate categories
        ViewBag.ProductList = new SelectList(db.Products.Select(p => p.Category).Distinct().ToList());
        return View();
    }
    // POST: Home/Insert
    [HttpPost]
    public IActionResult Insert(ProductInsertVM vm)
    {
        if (vm.PhotoURL != null)
        {
            var e = hp.ValidatePhoto(vm.PhotoURL);
            if (e != "") 
            {
                ModelState.AddModelError("PhotoFile", e);
            }
        }
        // ---id generation---
        // find the lastest id
        var maxId = db.Products
            .Where(i => i.Id.Length == 6) // id must be 6 characters
            .OrderByDescending(i => i.Id.Substring(1, 5))  // sort by 99999 to 00001
            .Select(i => (string?)i.Id)
            .FirstOrDefault();

        int currentNumber = 0;
        if (maxId != null)
        {
            // extract number and letter from the max ID
            string numPart = maxId.Substring(1, 5);
            //validate maxId format 
            if (!int.TryParse(numPart, out currentNumber))
            {
                ModelState.AddModelError("", "Internal error: Could not parse max Product Id number.");
            }
        }
        int nextNumber = currentNumber + 1;
        string newId = $"P{nextNumber.ToString("D5")}";


        string photoUrl = "";
        if (vm.PhotoURL != null)
        {
            photoUrl = hp.SavePhoto(vm.PhotoURL, "images");
        }
        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = new SelectList(db.Products.Select(p => p.Category).Distinct().ToList());
            return View(vm);
        }
        if (ModelState.IsValid)
        {
            db.Products.Add(new()
            {
                Id = newId,
                Name = vm.Name,
                SellPrice = vm.SellPrice,
                PhotoURL = photoUrl,
                Category = vm.Category,
                WareHouseQty = 0,
                StoreFrontQty = 0,
            });
            db.SaveChanges();

            TempData["Info"] = $"{newId} inserted.";
            return RedirectToAction("Index", "Product");
        }
        return View(vm);
    }
    public IActionResult Update(string? id)
    {
        var p = db.Products.Find(id);

        if (p == null)
        {
            return RedirectToAction("Index", "Product");
        }

        var vm = new ProductUpdateVM
        {
            Id = p.Id,
            Name = p.Name,
            SellPrice = p.SellPrice,
            PhotoURL = p.PhotoURL,
            Category = p.Category,
            WareHouseQty = p.WareHouseQty,
            StoreFrontQty = p.StoreFrontQty
        };
        ViewBag.ProductList = new SelectList(db.Products.Select(c => c.Category).Distinct().ToList(), vm.Category);
        return View(vm);
    }
    // POST: Product/Update
    [HttpPost]
    public IActionResult Update(ProductUpdateVM vm)
    {
        // 1. Validate NEW Photo (only if user uploaded one)
        if (vm.Photo != null)
        {
            var e = hp.ValidatePhoto(vm.Photo);
            if (e != "") ModelState.AddModelError("Photo", e);
        }

        if (ModelState.IsValid)
        {
            var p = db.Products.Find(vm.Id);
            if (p != null)
            {
                // 2. Update Info
                p.Name = vm.Name;
                p.SellPrice = vm.SellPrice;
                p.Category = vm.Category;
                p.WareHouseQty = vm.WareHouseQty;
                p.StoreFrontQty = vm.StoreFrontQty;

                // 3. Update Photo ONLY if a new one was uploaded
                if (vm.Photo != null)
                {
                    hp.DeletePhoto(p.PhotoURL, "images");
                    // Save new photo
                    p.PhotoURL = hp.SavePhoto(vm.Photo, "images");
                }

                db.SaveChanges();
                TempData["Info"] = $"Product {vm.Id} updated.";
                return RedirectToAction("Index", "Product");
            }
        }

        // If we reach here, something failed. Reload the dropdown.
        var dupCategories = db.Products.Select(c => c.Category).Distinct().ToList();
        ViewBag.ProductList = new SelectList(dupCategories, vm.Category);
        //vm.PhotoURL = p.PhotoURL;
        return View(vm);
    }
    // POST: Product/Delete
    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var p = db.Products.Find(id);

        if (p != null)
        {
            // 1. Delete the photo file from the "images" folder
            // (Make sure Helper.cs DeletePhoto is uncommented!)
            hp.DeletePhoto(p.PhotoURL, "images");

            // 2. Remove the record from the database
            db.Products.Remove(p);
            db.SaveChanges();

            TempData["Info"] = $"Product {p.Id} deleted.";
        }

        return RedirectToAction("Index", "Product");
    }
    // GET: Product/Detail/P00001
    public IActionResult Detail(string? id)
    {
        // Find the product by ID
        var p = db.Products.Find(id);

        // If ID is wrong or product deleted, go back to list
        if (p == null)
        {
            return RedirectToAction("Index", "Product");
        }

        // Pass the product directly to the View
        return View(p);
    }
}
