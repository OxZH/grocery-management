using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GroceryManagement.Controllers;

public class SupplierController(DB db) : Controller
{
    // Manually generate next id
    private string NextId()
    {
        string max = db.Suppliers.Max(t => t.Id) ?? "SUP000";
        int n = int.Parse(max[3..]);
        return (n + 1).ToString("'SUP'000");
    }

    private string NextSupplierTagId()
    {
        string max = db.SupplierTags.Max(t => t.Id) ?? "ST000";
        int n = int.Parse(max[2..]);
        return (n + 1).ToString("'ST'000");
    }

    public IActionResult Index()
    {
        return View(db.Suppliers);
    }

    public IActionResult Details(string? id)
    {
        var sup = db.Suppliers.Find(id);
        if (sup == null)
        {
            return RedirectToAction("Index");
        }

        return View(sup);
    }

    public IActionResult Insert()
    {
        ViewBag.NextId = NextId();
        ViewBag.SupplierTags = new MultiSelectList(db.SupplierTags, "Id", "Name");
        return View();
    }

    [HttpPost]
    public IActionResult Insert(SupplierVM vm, string[] tags)
    {
        if (ModelState.IsValid)
        {
            db.Suppliers.Add(new()
            {
                Id = NextId(),
                Name = vm.Name.Trim(),
                SupplierType = vm.SupplierType,
                Address = vm.Address.Trim(),
                ContactNo = vm.ContactNo,
                SupplierTags = db.SupplierTags
                       .Where(t => tags.Contains(t.Id))
                       .ToList()
            });
            db.SaveChanges();

            TempData["Info"] = "Record inserted.";
            return RedirectToAction("Index");
        }

        ViewBag.NextId = NextId();
        ViewBag.SupplierTags = new MultiSelectList(db.SupplierTags, "Id", "Name");
        return View();
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

        var selected = sup.SupplierTags.Select(t => t.Id);
        ViewBag.SupplierTags = new MultiSelectList(db.SupplierTags, "Id", "Name", selected);
        return View(vm);
    }

    [HttpPost]
    public IActionResult Update(SupplierVM vm, string[] tags)
    {
        var sup = db.Suppliers.Find(vm.Id);
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
        sup.SupplierTags = db.SupplierTags
                   .Where(t => tags.Contains(t.Id))
                   .ToList();

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

    // supplier tags
    public IActionResult Tags()
    {
        ViewBag.SupplierTags = db.SupplierTags;
        return View();
    }

    [HttpPost]
    public IActionResult Tags(SupplierTagVM vm)
    {
        if (ModelState.IsValid)
        {
            var id = NextSupplierTagId();
            db.SupplierTags.Add(new()
            {
                Id = id,
                Name = vm.Name
            });
            db.SaveChanges();

            TempData["Info"] = $"Tag {id} inserted.";
            return RedirectToAction("Tags");
        }

        ViewBag.SupplierTags = db.SupplierTags;
        return View(vm);
    }

    [HttpPost]
    public IActionResult DeleteTag(string? id)
    {
        var tag = db.SupplierTags.Find(id);
        if (tag != null)
        {
            db.SupplierTags.Remove(tag);
            db.SaveChanges();
            TempData["Info"] = "Tag deleted.";
        }
        return RedirectToAction("Tags");
    }
}
