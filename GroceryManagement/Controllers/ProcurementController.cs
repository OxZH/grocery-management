using GroceryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;

namespace GroceryManagement.Controllers;

public class ProcurementController(DB db, Helper hp) : Controller
{
    public IActionResult Index(string? sort, string? dir, int page = 1)
    {
        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        Func<ProcurementRecord, object> fn = sort switch
        {
            "Id" => proc => proc.Id,
            "ProcurementDateTime" => proc => proc.ProcurementDateTime,
            "StatusUpdateDateTime" => proc => proc.StatusUpdateDateTime ?? DateTime.MinValue,
            "TotalPrice" => proc => proc.TotalPrice,
            _ => proc => proc.Id,
        };

        var sorted = dir == "des" ?
                     db.ProcurementRecords.OrderByDescending(fn) :
                     db.ProcurementRecords.OrderBy(fn);

        // (3) Paging ---------------------------
        if (page < 1)
        {
            return RedirectToAction(null, new { sort, dir, page = 1 });
        }

        var m = sorted.ToPagedList(page, 10);
        if (page > m.PageCount && m.PageCount > 0)
        {
            return RedirectToAction(null, new { sort, dir, page = m.PageCount });
        }

        if (Request.IsAjax())
        {
            return PartialView("_ProcurementList", m);
        }

        return View(m);
    }

    // Manually generate next id
    private string NextId()
    {
        string max = db.ProcurementRecords.Max(t => t.Id) ?? "PR000000";
        int n = int.Parse(max[2..]);
        return (n + 1).ToString("'PR'000000");
    }

    public IActionResult Insert()
    {
        ViewBag.SupplierList = new SelectList(db.Suppliers, "Id", "Name");
        ViewBag.NextId = NextId();
        return View();
    }

    [HttpGet]
    public IActionResult GetProducts(string? supplierId)
    {
        var data = db.Products
            .Where(p => p.SupplierId == supplierId)
            .Select(p => new
            {
                value = p.Id,
                text = p.Name
            })
            .ToList();

        return Json(data);
    }

    [HttpGet]
    public IActionResult GetTotalPrice(string? productId, int quantity = 0)
    {
        var product = db.Products.FirstOrDefault(p => p.Id == productId);
        return Json(new { totalPrice = product?.Price * quantity });
    }

    public IActionResult Details(string? id)
    {
        var proc = db.ProcurementRecords.Find(id);
        if (proc == null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.Products = db.Products.Find(proc.ProductId);
        ViewBag.Suppliers = db.Suppliers.Find(proc.SupplierId);
        return View(proc);
    }

    [HttpPost]
    public IActionResult Insert(ProcurementRecordVM vm)
    {
        var product = db.Products.FirstOrDefault(p => p.Id == vm.ProductId);
        if (product == null)
        {
            ViewBag.SupplierList = new SelectList(db.Suppliers.ToList(), "Id", "Name");
            ViewBag.NextId = NextId();
            return View();
        }

        if (ModelState.IsValid)
        {
            var now = DateTime.Now;
            var supplier = db.Suppliers.First(p => p.Id == vm.SupplierId);
            db.ProcurementRecords.Add(new()
            {
                Id = NextId(),
                ProductId = vm.ProductId,
                Quantity = vm.Quantity,
                TotalPrice = vm.Quantity * product.Price,
                ProcurementDateTime = now,
                StatusUpdateDateTime = now,
                Status = "Ordered",
                SupplierId = vm.SupplierId,
            });
            db.SaveChanges();

            TempData["Info"] = "Record inserted.";
            return RedirectToAction("Index");
        }

        ViewBag.SupplierList = new SelectList(db.Suppliers, "Id", "Name");
        return View();
    }

    public IActionResult Update(string? id)
    {
        var record = db.ProcurementRecords.Find(id);
        if (record == null)
        {
            return RedirectToAction("Index");
        }
        var vm = new ProcurementRecordVM
        {
            Id = record.Id,
            ProductId = record.ProductId,
            Quantity = record.Quantity,
            SupplierId = record.SupplierId
        };

        ViewBag.SupplierList = new SelectList(db.Suppliers, "Id", "Name", record.SupplierId);
        ViewBag.ProductList = new SelectList(db.Products, "Id", "Name", record.ProductId);
        return View(vm);
    }

    [HttpPost]
    public IActionResult Update(ProcurementRecordVM vm)
    {
        var proc = db.ProcurementRecords.Find(vm.Id);
        if (proc == null)
        {
            return RedirectToAction("Index");
        }

        var product = db.Products.FirstOrDefault(p => p.Id == vm.ProductId);
        if (product == null)
        {
            return View(vm);
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        proc.SupplierId = vm.SupplierId;
        proc.ProductId = vm.ProductId;
        proc.Quantity = vm.Quantity;
        proc.TotalPrice = vm.Quantity * product.Price;
        db.SaveChanges();

        TempData["Info"] = "Record updated.";
        return RedirectToAction("Index");
    }

    public IActionResult UpdateStatus(string? id)
    {
        var record = db.ProcurementRecords.Find(id);
        if (record == null)
        {
            return RedirectToAction("Index");
        }

        var vm = new ProcurementRecordVM
        {
            Id = record.Id,
            ProductId = record.ProductId,
            Quantity = record.Quantity,
            SupplierId = record.SupplierId,
            TotalPrice = record.TotalPrice,
            Status = record.Status
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult UpdateStatus(ProcurementRecordVM vm)
    {
        var record = db.ProcurementRecords.Find(vm.Id);
        if (record == null)
        {
            return View(vm);
        }

        if (vm.Status == "Received")
        {
            if (vm.ProofPhoto == null)
            {
                ModelState.AddModelError("Photo", "Please upload a photo of the receipt as proof.");
            }
            else
            {
                var e = hp.ValidatePhoto(vm.ProofPhoto);
                if (e != "")
                {
                    ModelState.AddModelError("Photo", e);
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        if (vm.ProofPhoto != null)
        {
            record.DeliveryProofPhotoLink = hp.SavePhoto(vm.ProofPhoto, "images/procurement_proof");
        }
        record.Status = vm.Status;
        record.StatusUpdateDateTime = DateTime.Now;
        
        db.SaveChanges();

        TempData["Info"] = "Status updated.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Delete(string? id)
    {
        var record = db.ProcurementRecords.Find(id);
        if (record != null)
        {
            db.ProcurementRecords.Remove(record);
            db.SaveChanges();
            TempData["Info"] = "Record deleted.";
        }
        return RedirectToAction("Index");
    }
}
