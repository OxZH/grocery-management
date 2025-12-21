using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GroceryManagement.Models;

namespace GroceryManagement.Controllers;

[Authorize(Roles = "Manager")]
public class FinanceController : Controller
{
    private readonly DB _db;
    public FinanceController(DB db) => _db = db;

    public IActionResult Index()
    {
        ViewBag.Title = "Finance";
        return View();
    }

    public IActionResult Invoices()
    {
        ViewBag.Title = "Invoices";
        var vm = new FinanceInvoicesVM
        {
            Checkouts = _db.Set<Checkout>().OrderByDescending(c => c.Date).Take(200).ToList(),
            Procurements = _db.Set<ProcurementRecord>().OrderByDescending(p => p.ProcurementDateTime).Take(200).ToList()
        };
        return View(vm);
    }

    [HttpGet]
    public IActionResult CheckoutTrend(string period = "week")
    {
        var now = DateTime.Now.Date;
        var lower = period?.ToLower() ?? "week";
        var byMonth = lower == "year";

        DateTime start = lower switch
        {
            "year" => new DateTime(now.Year, 1, 1),
            "month" => new DateTime(now.Year, now.Month, 1),
            _ => now.AddDays(-6) // week: last 7 days including today
        };

        if (byMonth)
        {
            var data = _db.Set<Checkout>()
                .AsNoTracking()
                .Where(c => c.Date.Date >= start && c.Date.Date <= now)
                .GroupBy(c => new { c.Date.Year, c.Date.Month })
                .Select(g => new { Key = new DateTime(g.Key.Year, g.Key.Month, 1), Count = g.Count() })
                .ToList();

            var labels = new List<string>();
            var counts = new List<int>();
            var iter = new DateTime(start.Year, start.Month, 1);
            var end = new DateTime(now.Year, now.Month, 1);
            while (iter <= end)
            {
                labels.Add(iter.ToString("yyyy-MM"));
                var found = data.FirstOrDefault(x => x.Key == iter);
                counts.Add(found?.Count ?? 0);
                iter = iter.AddMonths(1);
            }
            return Json(new { labels, counts });
        }
        else
        {
            var data = _db.Set<Checkout>()
                .AsNoTracking()
                .Where(c => c.Date.Date >= start && c.Date.Date <= now)
                .GroupBy(c => c.Date.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToList();

            // fill gaps daily
            var labels = new List<string>();
            var counts = new List<int>();
            for (var d = start; d <= now; d = d.AddDays(1))
            {
                labels.Add(d.ToString("yyyy-MM-dd"));
                var found = data.FirstOrDefault(x => x.Date == d);
                counts.Add(found?.Count ?? 0);
            }
            return Json(new { labels, counts });
        }
    }

    public IActionResult InvoicesPartial(string search, string sort = "date", string dir = "desc", int page = 1, int pageSize = 20)
    {
        var vm = BuildInvoicesVM(search, sort, dir, page, pageSize, out var totalPages, out var totalCount);
        ViewBag.PageNumber = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        return PartialView("_InvoicesTable", vm);
    }

    public IActionResult Expenses()
    {
        ViewBag.Title = "Expenses";
        // placeholder: Expense model currently minimal
        var expenses = _db.Set<Expense>().Include(e => e.Manager).OrderByDescending(e => e.Date).Take(200).ToList();
        ViewBag.TotalPages = 1;
        ViewBag.PageNumber = 1;
        ViewBag.TotalCount = expenses.Count;
        return View(expenses);
    }

    public IActionResult ExpensesPartial(string search, string sort = "date", string dir = "desc", int page = 1, int pageSize = 20)
    {
        var q = _db.Set<Expense>().Include(e => e.Manager).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(e => e.Id.Contains(s) || e.Type.Contains(s) || e.Details.Contains(s));
        }
        q = (sort.ToLower(), dir.ToLower()) switch
        {
            ("amount", "asc") => q.OrderBy(e => e.Amount),
            ("amount", _) => q.OrderByDescending(e => e.Amount),
            ("date", "asc") => q.OrderBy(e => e.Date),
            ("date", _) => q.OrderByDescending(e => e.Date),
            _ => q.OrderByDescending(e => e.Date)
        };
        var total = q.Count();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var list = q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        ViewBag.PageNumber = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = total;
        return PartialView("_ExpensesTable", list);
    }

    public IActionResult Reports()
    {
        ViewBag.Title = "Finance Reports";
        var revenue = _db.Set<Checkout>().Sum(c => c.Total);
        var expenseTotal = _db.Set<Expense>().Sum(e => e.Amount);
        var procurementCost = _db.Set<ProcurementRecord>().Sum(p => p.TotalPrice);
        var vm = new FinanceReportVM
        {
            Revenue = revenue,
            ExpenseTotal = expenseTotal,
            ProcurementCost = procurementCost,
            NetProfit = revenue - expenseTotal - procurementCost
        };
        return View(vm);
    }

    public IActionResult Invoice(string type, string id)
    {
        if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(id)) return NotFound();
        type = type.ToLowerInvariant();
        var vm = new FinanceInvoiceDetailVM { InvoiceType = type == "procurement" ? "Procurement" : "Order" };
        if (type == "procurement")
        {
            vm.Procurement = _db.Set<ProcurementRecord>().Include(p => p.Supplier).FirstOrDefault(p => p.Id == id);
            if (vm.Procurement == null) return NotFound();
        }
        else
        {
            vm.Checkout = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
            if (vm.Checkout == null) return NotFound();
        }
        ViewBag.Title = "Invoice " + id;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmProcurementPayment(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();
        var pr = _db.Set<ProcurementRecord>().FirstOrDefault(p => p.Id == id);
        if (pr == null) return NotFound();
        pr.Status = "PAID";
        pr.StatusUpdateDateTime = DateTime.Now;
        _db.SaveChanges();
        TempData["Info"] = $"Procurement {id} marked as paid.";
        return RedirectToAction(nameof(Invoices));
    }

    private FinanceInvoicesVM BuildInvoicesVM(string search, string sort, string dir, int page, int pageSize, out int totalPages, out int totalCount)
    {
        var orders = _db.Set<Checkout>().AsQueryable();
        var procs = _db.Set<ProcurementRecord>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            orders = orders.Where(c => c.Id.Contains(s) || c.CustomerId.Contains(s));
            procs = procs.Where(p => p.Id.Contains(s) || p.SupplierId.Contains(s));
        }

        orders = (sort.ToLower(), dir.ToLower()) switch
        {
            ("total", "asc") => orders.OrderBy(c => c.Total),
            ("total", _) => orders.OrderByDescending(c => c.Total),
            ("date", "asc") => orders.OrderBy(c => c.Date),
            ("date", _) => orders.OrderByDescending(c => c.Date),
            _ => orders.OrderByDescending(c => c.Date)
        };

        procs = (sort.ToLower(), dir.ToLower()) switch
        {
            ("total", "asc") => procs.OrderBy(p => p.TotalPrice),
            ("total", _) => procs.OrderByDescending(p => p.TotalPrice),
            ("date", "asc") => procs.OrderBy(p => p.ProcurementDateTime),
            ("date", _) => procs.OrderByDescending(p => p.ProcurementDateTime),
            _ => procs.OrderByDescending(p => p.ProcurementDateTime)
        };

        // combine counts for paging lists separately; keep same page for both but capped size
        var ordersTotal = orders.Count();
        var procsTotal = procs.Count();
        totalCount = ordersTotal + procsTotal;
        totalPages = (int)Math.Ceiling(Math.Max(ordersTotal, procsTotal) / (double)pageSize);

        var vm = new FinanceInvoicesVM
        {
            Checkouts = orders.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Procurements = procs.Skip((page - 1) * pageSize).Take(pageSize).ToList()
        };
        return vm;
    }
}
