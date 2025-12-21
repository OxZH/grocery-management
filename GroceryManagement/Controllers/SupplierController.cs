using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GroceryManagement.Controllers;

public class SupplierController(DB db) : Controller
{
    // Manually generate next id
    private string NextId()
    {
        string max = db.Supplier.Max(t => t.Id) ?? "SUP000";
        int n = int.Parse(max[3..]);
        return (n + 1).ToString("'SUP'000");
    }

    public IActionResult Index()
    {
        return View(db.Supplier);
    }

    public IActionResult Details(string? id)
    {
        var sup = db.Supplier.Find(id);
        if (sup == null)
        {
            return RedirectToAction("Index");
        }

        return View(sup);
    }

    public IActionResult Insert()
    {
        ViewBag.NextId = NextId();
        return View();
    }

    [HttpPost]
    public IActionResult Insert(SupplierVM vm)
    {
        if (ModelState.IsValid)
        {
            var nextId = NextId();
            db.Supplier.Add(new()
            {
                Id = nextId,
                Name = vm.Name.Trim(),
                SupplierType = vm.SupplierType,
                Address = vm.Address.Trim(),
                ContactNo = vm.ContactNo,
            });
            db.SaveChanges();

            TempData["Info"] = "Record inserted.";
            return RedirectToAction("Index");
        }

        ViewBag.NextId = NextId();
        return View(vm);
    }

    public IActionResult Update(string? id)
    {
        var sup = db.Supplier.Find(id);
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
        var sup = db.Supplier.Find(vm.Id);
        if (sup == null)
        {
            return RedirectToAction("Index");
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        sup.Name = vm.Name.Trim();
        sup.SupplierType = vm.SupplierType;
        sup.Address = vm.Address.Trim();
        sup.ContactNo = vm.ContactNo;
        db.SaveChanges();

        TempData["Info"] = "Record updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var sup = db.Supplier.Find(id);
        if (sup != null)
        {
            db.Supplier.Remove(sup);
            db.SaveChanges();
            TempData["Info"] = "Record deleted.";
        }
        return RedirectToAction("Index");
    }
}
