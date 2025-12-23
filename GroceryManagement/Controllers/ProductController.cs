using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using X.PagedList.Extensions;

namespace GroceryManagement.Controllers;

[Authorize]
public class ProductController(DB db, Helper hp) : Controller
{
    public IActionResult Index(string? name, string? sort, string? dir, int page = 1)
    {

        ViewBag.Name = name = name?.Trim() ?? "";

        var searched = db.Products.Where(s =>
        s.Name.Contains(name) ||
        s.Category.Contains(name) ||
        s.Id.Contains(name));
 

        ViewBag.Sort = sort;
        ViewBag.Dir = dir;
        Func<Product, object> fn = sort switch
        {
            "Id" => s => s.Id,
            "Name" => s => s.Name,
            "Category" => s => s.Category,
            "WareHouseQty" => s => s.WareHouseQty,
            "StoreFrontQty" => s => s.StoreFrontQty,
            _ => s => s.Id,
        };
        var dataList = searched.AsEnumerable();
        Func<Product, bool> isZeroStock = p => p.WareHouseQty == 0 || p.StoreFrontQty == 0;
        Func<Product, bool> isLowStock = p => p.WareHouseQty < 20 || p.StoreFrontQty < 20;

        var sorted = dir == "des" ?
            dataList.OrderByDescending(isZeroStock).ThenByDescending(isLowStock).ThenByDescending(fn) :
            dataList.OrderByDescending(isZeroStock).ThenByDescending(isLowStock).ThenBy(fn);

        if (page < 1)
        {
            return RedirectToAction(null, new { name, sort, dir, page = 1 });
        }

        var m = sorted.ToPagedList(page, 10);

        if (page > m.PageCount && m.PageCount > 0)
        {
            return RedirectToAction(null, new { name, sort, dir, page = m.PageCount });
        }
        if (Request.IsAjax())
        {
            return PartialView("_Index", m);
        }
        return View(m);

    }


    public IActionResult Insert()
    {
        // filter duplicate categories
        ViewBag.ProductList = db.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
        return View();
    }
    // POST: Home/Insert
    [HttpPost]
    public IActionResult Insert(ProductInsertVM vm, string? Direction, bool Flip)
    {
        vm.Name = vm.Name?.Trim().ToUpper();
        vm.Category = vm.Category?.Trim().ToUpper();
        if (vm.PhotoURL != null)
        {
            var e = hp.ValidatePhoto(vm.PhotoURL);
            if (e != "") 
            {
                ModelState.AddModelError("PhotoURL", e);
            }
        }
        if (db.Products.Any(p => p.Name == vm.Name))
        {
            ModelState.AddModelError("Name", "Product Name already exists.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = db.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
            return View(vm);
        }
        // find the lastest id
        var maxId = db.Products
            .Where(i => i.Id.Length == 6) 
            .OrderByDescending(i => i.Id.Substring(1, 5))
            .Select(i => (string?)i.Id)
            .FirstOrDefault();

        int currentNumber = 0;
        if (maxId != null)
        {
            // extract number and letter from the max id
            string numPart = maxId.Substring(1, 5);
            //validate max id format 
            if (!int.TryParse(numPart, out currentNumber))
            {
                ModelState.AddModelError("", "Internal error: Could not parse max Product Id number.");
            }
        }
        int nextNumber = currentNumber + 1;
        string newId = $"P{nextNumber.ToString("D5")}";

        int degrees = 0;
        if (Direction == "Left") degrees = -90;
        else if (Direction == "Right") degrees = 90;

        string photoUrl = "";
        if (vm.PhotoURL != null)
        {
            photoUrl = hp.ProSavePhoto(vm.PhotoURL, "images/productImg", degrees, Flip);
        }
        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = db.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToList();
            return View(vm);
        }
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
    public IActionResult Update(ProductUpdateVM vm, string? Direction, bool Flip)
    {
        vm.Name = vm.Name?.Trim().ToUpper();
        vm.Category = vm.Category?.Trim().ToUpper();
        // Validate NEW Photo
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
                // update Info
                p.Name = vm.Name;
                p.SellPrice = vm.SellPrice;
                p.Category = vm.Category;
                p.WareHouseQty = vm.WareHouseQty;
                p.StoreFrontQty = vm.StoreFrontQty;

                //ROTATION LOGIC
                int degrees = 0;
                if (Direction == "Left") degrees = -90;
                else if (Direction == "Right") degrees = 90;

                if (vm.Photo != null)
                {
                    if (!string.IsNullOrEmpty(p.PhotoURL))
                    {
                        hp.DeletePhoto(p.PhotoURL, "images/productImg");
                    }

                    // --- 3. USE 'ProSavePhoto' INSTEAD OF 'SavePhoto' ---
                    p.PhotoURL = hp.ProSavePhoto(vm.Photo, "images/productImg", degrees, Flip);
                }
                else if (!string.IsNullOrEmpty(p.PhotoURL) && (degrees != 0 || Flip))
                {
                    // No new file, but User requested rotation on the CURRENT file
                    p.PhotoURL = hp.ProCurrentPhoto(p.PhotoURL, "images/productImg", degrees, Flip);
                }

                db.SaveChanges();
                TempData["Info"] = $"Product {vm.Id} updated.";
                return RedirectToAction("Index", "Product");
            }
        }

        var dupCategories = db.Products.Select(c => c.Category).Distinct().ToList();
        ViewBag.ProductList = new SelectList(dupCategories, vm.Category);
        return PartialView(vm);
    }
    // POST: Product/Delete
    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var p = db.Products.Find(id);

        if (p != null)
        {
            bool inUse = db.Inventories.Any(i => i.ProductId == id);

            if (inUse)
            {
                TempData["Info"] = $"Delete Failed: Product {id} is currently used in Inventory.";
                return RedirectToAction("Index", "Product");
            }
            //delete photo
            hp.DeletePhoto(p.PhotoURL, "images/productImg");

            // remove the record from the database
            db.Products.Remove(p);
            db.SaveChanges();

            TempData["Info"] = $"Product {id} deleted.";
            return RedirectToAction("Index", "Product");
        }
        TempData["Info"] = "Product not found.";
        return RedirectToAction("Index", "Product");
    }
    // GET: Product/Detail/
    public IActionResult Detail(string? id)
    {
        // Find the product by ID
        var p = db.Products.Find(id);
        if (p == null)
        {
            return RedirectToAction("Index", "Product");
        }
        return View(p);
    }
    public IActionResult MoveStock(string? id)
    {
        var p = db.Products.Find(id);
        if (p == null)
        {
            return RedirectToAction("Index", "Product");
        }
        var vm = new ProductMoveVM
        {
            Id = p.Id,
            Name = p.Name,
            PhotoURL = p.PhotoURL,
            WarehouseQty = p.WareHouseQty,
            StoreFrontQty = p.StoreFrontQty,
            QtyToMove = 1
        };

        return View(vm);
    }
    [HttpPost]
    public IActionResult MoveStock(ProductMoveVM vm)
    {

        var p = db.Products.Find(vm.Id);
        if (p == null)
        {
            return RedirectToAction("Index", "Product");
        }
        if (vm.QtyToMove < 1)
        {
            ModelState.AddModelError("QtyToMove", "Quantity must be greater than 0.");
        }

        //validate warehouse qty
        if (p.WareHouseQty < vm.QtyToMove)
        {
            ModelState.AddModelError("QtyToMove", $"Not enough stock in Warehouse. Current: {p.WareHouseQty}");
           
        }
        if (!ModelState.IsValid)
        {
            vm.Name = p.Name;
            vm.PhotoURL = p.PhotoURL;
            vm.WarehouseQty = p.WareHouseQty;
            vm.StoreFrontQty = p.StoreFrontQty;
            return View(vm);
        }
        // move to store front
        p.WareHouseQty -= vm.QtyToMove;
        p.StoreFrontQty += vm.QtyToMove;
        db.SaveChanges();

        TempData["Info"] = $"Success: Moved {vm.QtyToMove} units. New Warehouse Qty: {p.WareHouseQty}";
        return RedirectToAction("Index", "Product");
    }
}
