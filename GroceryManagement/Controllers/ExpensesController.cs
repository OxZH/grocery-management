using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroceryManagement.Controllers;

[Authorize(Roles = "Manager")]
public class ExpensesController : Controller
{
    private readonly DB _db;
    private readonly Helper _helper;
    public ExpensesController(DB db, Helper helper)
    {
        _db = db;
        _helper = helper;
    }

    // GET: /Expenses
    public IActionResult Index()
    {
        var list = _db.Set<Expense>()
            .AsNoTracking()
            .OrderByDescending(e => e.Date)
            .ToList();
        return View(list);
    }

    // GET: /Expenses/Create
    public IActionResult Create(string type)
    {
        var model = new Expense { Date = DateTime.Today, Type = string.IsNullOrWhiteSpace(type) ? "Utilities" : type };
        return View(model);
    }

    // POST: /Expenses/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Expense model)
    {
        // capture payment method from form (no dedicated column)
        var paymentMethod = (Request?.Form?["PaymentMethod"].ToString() ?? "Cash").Trim();

        // auto-set date to today
        model.Date = DateTime.Today;

        // no salary option; always clear staff
        model.StaffId = null;

        // only store user details; if empty, fall back to payment method text
        var detail = (model.Details ?? string.Empty).Trim();
        model.Details = string.IsNullOrEmpty(detail)
            ? paymentMethod
            : detail;

        // re-validate after adjustments
        ModelState.Clear();
        TryValidateModel(model);

        // auto-generate Id if not provided (EX + 3 digits to fit legacy 5-char column)
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            var next = _helper.GetNextExpenseSequence(_db);
            model.Id = "EX" + next.ToString("0000");
        }
        else if (_db.Set<Expense>().Any(e => e.Id == model.Id))
        {
            ModelState.AddModelError("Id", "An expense with this Id already exists");
        }

        // clear modelstate errors for Id/Manager (we set them server-side)
        ModelState.Remove("Id");
        ModelState.Remove("Manager");
        ModelState.Remove("ManagerId");

        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var entry = _db.Entry(model);
            var mgrShadow = entry.Property<string>("ManagerId");
            if (mgrShadow != null && string.IsNullOrWhiteSpace(mgrShadow.CurrentValue))
            {
                mgrShadow.CurrentValue = "M002";
            }

            _db.Add(model);
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Database error: " + ex.Message);
            return View(model);
        }
        TempData["Info"] = $"Expense {model.Id} created.";
        var referer = Request?.Headers?["Referer"].FirstOrDefault();
        if (!string.IsNullOrEmpty(referer)) return Redirect(referer);
        return RedirectToAction(nameof(Index));
    }

    // GET: /Expenses/Edit/{id}
    public IActionResult Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var exp = _db.Set<Expense>().FirstOrDefault(e => e.Id == id);
        if (exp == null) return NotFound();
        return View(exp);
    }

    // POST: /Expenses/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Expense model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var exp = _db.Set<Expense>().FirstOrDefault(e => e.Id == model.Id);
        if (exp == null) return NotFound();

        exp.Type = model.Type;
        exp.Details = model.Details;
        exp.Date = model.Date;
        exp.Amount = model.Amount;
        // ensure manager shadow default if missing
        var mgrShadow = _db.Entry(exp).Property<string>("ManagerId");
        if (mgrShadow != null && string.IsNullOrWhiteSpace(mgrShadow.CurrentValue))
        {
            mgrShadow.CurrentValue = "M002";
        }
        _db.SaveChanges();
        TempData["Info"] = $"Expense {model.Id} updated.";
        var referer = Request?.Headers?["Referer"].FirstOrDefault();
        if (!string.IsNullOrEmpty(referer)) return Redirect(referer);
        return RedirectToAction(nameof(Index));
    }

    // POST: /Expenses/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var exp = _db.Set<Expense>().FirstOrDefault(e => e.Id == id);
        if (exp == null) return NotFound();
        _db.Remove(exp);
        _db.SaveChanges();
        TempData["Info"] = $"Expense {id} deleted.";
        return RedirectToAction(nameof(Index));
    }
}
