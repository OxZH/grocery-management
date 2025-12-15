using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GroceryManagement.Controllers;

public class SupplierController(DB db) : Controller
{
    public bool CheckId(string id)
    {
        return !db.Suppliers.Any(s => s.Id == id);
    }

    public IActionResult Index()
    {
        return View(db.Suppliers);
    }

    public IActionResult Insert()
    {
        return View();
    }

    // POST: Home/Insert
    [HttpPost]
    public IActionResult Insert(SupplierVM vm)
    {
        if (ModelState.IsValid("Id") && db.Suppliers.Any(s => s.Id == vm.Id))
        {
            ModelState.AddModelError("Id", "Duplicated ID.");
        }

        if (ModelState.IsValid)
        {
            db.Suppliers.Add(new()
            {
                Id = vm.Id.Trim().ToUpper(),
                Name = vm.Name,
                SupplierType = vm.SupplierType,
                Address = vm.Address,
                ContactNo = vm.ContactNo,
            });
            db.SaveChanges();
        }

        TempData["Info"] = "Record inserted.";
        return RedirectToAction("Index");
    }

    public IActionResult Update(string? id)
    {
        var sup = db.Suppliers.Find(id);
        if (sup == null)
        {
            return RedirectToAction("Index");
        }

        var vm = new SupplierVM
        {
            Id = sup.Id,
            Name = sup.Name,
            SupplierType = sup.SupplierType,
            Address = sup.Address,
            ContactNo = sup.ContactNo,
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult Update(SupplierVM vm)
    {
        var sup = db.Suppliers.Find(vm.Id);
        if (sup == null)
        {
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            return View();
        }

        sup.Name = vm.Name;
        sup.SupplierType = vm.SupplierType;
        sup.Address = vm.Address;
        sup.ContactNo = vm.ContactNo;
        db.SaveChanges();

        TempData["Info"] = "Record updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var sup = db.Suppliers.Find(id);
        if (sup != null)
        {
            db.Suppliers.Remove(sup);
            db.SaveChanges();
            TempData["Info"] = "Record deleted.";
        }
        return RedirectToAction("Index");
    }
}
