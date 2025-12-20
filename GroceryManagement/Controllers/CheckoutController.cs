using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using GroceryManagement.Models;

namespace GroceryManagement.Controllers;

public class CheckoutController : Controller
{
    private readonly DB _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CheckoutController> _logger;
    private static readonly string[] Workflow = new[] { "PENDING", "PICKING", "PACKED", "READY", "COMPLETED", "CANCELLED" };

    public CheckoutController(DB db, IWebHostEnvironment env, ILogger<CheckoutController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    // GET: /Checkout
    public IActionResult Index(string search, string statusFilter, string sortBy = "Date", string sortDir = "desc")
    {
        var list = QueryCheckouts(search, statusFilter, sortBy, sortDir, page: 1, pageSize: 20, out var totalPages, out var totalCount);
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.PageNumber = 1;
        ViewBag.Search = search;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.SortBy = sortBy;
        ViewBag.SortDir = sortDir;
        return View(list);
    }

    // GET: /Checkout/ListPartial
    public IActionResult ListPartial(string search, string statusFilter, string sortBy = "Date", string sortDir = "desc", int page = 1, int pageSize = 20)
    {
        var list = QueryCheckouts(search, statusFilter, sortBy, sortDir, page, pageSize, out var totalPages, out var totalCount);
        ViewBag.TotalPages = totalPages;
        ViewBag.TotalCount = totalCount;
        ViewBag.PageNumber = page;
        ViewBag.Search = search;
        ViewBag.StatusFilter = statusFilter;
        ViewBag.SortBy = sortBy;
        ViewBag.SortDir = sortDir;
        return PartialView("_CheckoutTable", list);
    }

    // GET: /Checkout/Create
    public IActionResult Create()
    {
        ViewBag.Workflow = Workflow;
        ViewBag.Products = _db.Set<Product>().OrderBy(p => p.Name).Take(200).ToList();
        return View(new CheckoutCreateVM());
    }

    private List<Checkout> QueryCheckouts(string search, string statusFilter, string sortBy, string sortDir, int page, int pageSize, out int totalPages, out int totalCount)
    {
        var q = _db.Set<Checkout>().AsQueryable();
        if (!string.IsNullOrEmpty(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.Id.Contains(s) || c.CustomerId.Contains(s) || c.InventoryId.Contains(s));
        }
        if (!string.IsNullOrEmpty(statusFilter))
        {
            q = q.Where(c => c.Status == statusFilter);
        }

        q = (sortBy?.ToLower(), sortDir?.ToLower()) switch
        {
            ("date", "asc") => q.OrderBy(c => c.Date),
            ("date", _) => q.OrderByDescending(c => c.Date),
            ("total", "asc") => q.OrderBy(c => c.Total),
            ("total", _) => q.OrderByDescending(c => c.Total),
            _ => q.OrderByDescending(c => c.Date),
        };

        totalCount = q.Count();
        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Max(1, page);
        return q.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    // POST: /Checkout/Create
    [HttpPost]
    public IActionResult Create(CheckoutCreateVM model)
    {
        if (model == null) return BadRequest();

        ViewBag.Workflow = Workflow;
        ViewBag.Products = _db.Set<Product>().OrderBy(p => p.Name).Take(200).ToList();

        var items = model.Items?.Where(i => !string.IsNullOrWhiteSpace(i.ProductId) && i.Quantity > 0).ToList() ?? new List<CheckoutItemInputVM>();
        if (items.Count == 0)
        {
            ModelState.AddModelError("Items", "Add at least one product with quantity.");
        }

        // normalize and validate CustomerId: accept digits or values starting with CUST; store as CUST + 4 digits
        var rawCust = model.CustomerId?.Trim() ?? string.Empty;
        var custDigits = new string(rawCust.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(custDigits) || custDigits.Length > 4)
        {
            ModelState.AddModelError("CustomerId", "CustomerId must be 1-4 digits (will be stored as CUSTxxxx)");
        }
        else
        {
            model.CustomerId = "CUST" + custDigits.PadLeft(4, '0');
        }

        var productIds = items.Select(i => i.ProductId.Trim().ToUpper()).ToList();
        var products = _db.Set<Product>().Where(p => productIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p);

        foreach (var i in items)
        {
            i.ProductId = i.ProductId.Trim().ToUpper();
            if (!products.ContainsKey(i.ProductId))
            {
                ModelState.AddModelError("Items", $"Product {i.ProductId} not found.");
            }
        }

        // allocate inventories for requested quantities
        var allocations = new List<Inventory>();
        foreach (var i in items)
        {
            if (!products.TryGetValue(i.ProductId, out var prod)) continue;
            var available = _db.Set<Inventory>()
                .Where(inv => inv.ProductId == i.ProductId && inv.CheckoutId == null && inv.Status == "AVAILABLE")
                .OrderBy(inv => inv.ExpiryDate)
                .Take(i.Quantity)
                .ToList();

            if (available.Count < i.Quantity)
            {
                ModelState.AddModelError("Items", $"Not enough inventory for product {i.ProductId}. Requested {i.Quantity}, available {available.Count}.");
            }
            allocations.AddRange(available);
        }

        // generate a new Id based on existing Checkout table (CH followed by 4 digits)
        var existing = _db.Set<Checkout>().Select(c => c.Id).ToList();
        var max = 0;
        foreach (var ex in existing)
        {
            if (string.IsNullOrEmpty(ex)) continue;
            var s = ex.StartsWith("CH", StringComparison.OrdinalIgnoreCase) ? ex.Substring(2) : ex;
            if (int.TryParse(s, out var n)) max = Math.Max(max, n);
        }
        var nextNum = max + 1;
        var checkoutId = "CH" + nextNum.ToString().PadLeft(4, '0');
        if (_db.Set<Checkout>().Any(c => c.Id == checkoutId))
        {
            ModelState.AddModelError("Id", "Order with this Id already exists");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var status = string.IsNullOrWhiteSpace(model.Status) ? "PENDING" : model.Status.ToUpperInvariant();
        if (!Workflow.Contains(status)) status = "PENDING";

        var checkout = new Checkout
        {
            Id = checkoutId,
            CustomerId = model.CustomerId,
            InventoryId = allocations.FirstOrDefault()?.Id ?? string.Empty,
            Total = 0,
            Date = DateTime.Now,
            Status = status,
            StatusUpdateDate = DateTime.Now,
            PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "CASH" : model.PaymentMethod.Trim().ToUpperInvariant()
        };

        try
        {
            var entry = _db.Entry(checkout);
            var staffShadow = entry.Property<string>("StaffId");
            if (staffShadow != null && string.IsNullOrWhiteSpace(staffShadow.CurrentValue))
            {
                staffShadow.CurrentValue = "S001";
            }

            _db.Add(checkout);
            _db.SaveChanges();

            decimal total = 0m;
            foreach (var group in items)
            {
                if (!products.TryGetValue(group.ProductId, out var prod)) continue;
                var required = group.Quantity;
                var taken = allocations.Where(a => a.ProductId == group.ProductId && a.CheckoutId == null).Take(required).ToList();
                foreach (var inv in taken)
                {
                    inv.CheckoutId = checkout.Id;
                    inv.Status = "SOLD";
                }
                total += prod.Price * taken.Count;
            }

            // ensure primary InventoryId not empty if we allocated something
            if (!string.IsNullOrEmpty(checkout.InventoryId) && !_db.Set<Inventory>().Any(i => i.Id == checkout.InventoryId))
            {
                checkout.InventoryId = allocations.FirstOrDefault()?.Id ?? checkout.InventoryId;
            }

            checkout.Total = total;
            checkout.StatusUpdateDate = DateTime.Now;
            _db.SaveChanges();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert checkout {id}", checkout.Id);
            ModelState.AddModelError("", "Failed to create order - database error: " + ex.Message);
            return View(model);
        }

        TempData["Info"] = $"Order {checkout.Id} created";
        var referer = Request?.Headers?["Referer"].FirstOrDefault();
        if (!string.IsNullOrEmpty(referer)) return Redirect(referer);
        return RedirectToAction(nameof(Details), new { id = checkout.Id });
    }

    // POST: /Checkout/Delete
    [HttpPost]
    public IActionResult Delete(string id)
    {
        if (string.IsNullOrEmpty(id)) return BadRequest();
        var o = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
        if (o == null) return NotFound();
        _db.Remove(o);
        _db.SaveChanges();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Checkout/Edit/{id}
    public IActionResult Edit(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var o = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
        if (o == null) return NotFound();
        ViewBag.Workflow = Workflow;
        return View(o);
    }

    // POST: /Checkout/Edit
    [HttpPost]
    public IActionResult Edit(Checkout model)
    {
        if (model == null || string.IsNullOrEmpty(model.Id)) return BadRequest();
        var o = _db.Set<Checkout>().FirstOrDefault(c => c.Id == model.Id);
        if (o == null) return NotFound();
        o.CustomerId = model.CustomerId;
        o.InventoryId = model.InventoryId;
        o.Total = model.Total;
        o.Status = model.Status;
        o.StatusUpdateDate = DateTime.Now;
        // ensure staff shadow default if missing
        var staffShadow = _db.Entry(o).Property<string>("StaffId");
        if (staffShadow != null && string.IsNullOrWhiteSpace(staffShadow.CurrentValue))
        {
            staffShadow.CurrentValue = "S001";
        }
        _db.SaveChanges();
        var referer = Request?.Headers?["Referer"].FirstOrDefault();
        if (!string.IsNullOrEmpty(referer)) return Redirect(referer);
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    // GET: /Checkout/Details/{id}
    public IActionResult Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var order = _db.Set<Checkout>()
            .Include(c => c.Inventories)
            .ThenInclude(i => i.Product)
            .FirstOrDefault(c => c.Id == id);
        if (order == null) return NotFound();

        var timeline = ReadTimeline(order.Id);
        ViewBag.Timeline = timeline;
        ViewBag.Workflow = Workflow;
        return View(order);
    }

    [HttpPost]
    public IActionResult UpdateStatus(string id, string newStatus)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus)) return BadRequest();
        newStatus = newStatus.ToUpperInvariant();
        var order = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
        if (order == null) return NotFound();

        // Validate transition
        var currentIdx = Array.IndexOf(Workflow, order.Status?.ToUpperInvariant() ?? "PENDING");
        var nextIdx = Array.IndexOf(Workflow, newStatus);
        if (nextIdx == -1)
        {
            return BadRequest(new { error = "Invalid status" });
        }

        // allow any forward move or cancellation from any state
        if (newStatus == "CANCELLED" || nextIdx >= currentIdx)
        {
            order.Status = newStatus;
            order.StatusUpdateDate = DateTime.Now;
            _db.SaveChanges();

            AppendTimeline(order.Id, newStatus);
            return Ok(new { success = true, status = newStatus });
        }

        return BadRequest(new { error = "Invalid transition" });
    }

    // GET: /Checkout/PackingSlip/{id}
    public IActionResult PackingSlip(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var order = _db.Set<Checkout>()
            .Include(c => c.Inventories)
            .ThenInclude(i => i.Product)
            .FirstOrDefault(c => c.Id == id);
        if (order == null) return NotFound();
        return View(order);
    }

    // GET: /Checkout/Notifications
    public IActionResult Notifications(DateTime? since)
    {
        var q = _db.Set<Checkout>().AsQueryable();
        if (since.HasValue)
        {
            q = q.Where(c => c.Date > since.Value);
        }
        var pending = q.Where(c => c.Status == "PENDING").OrderByDescending(c => c.Date).Take(10).ToList();
        return Json(new { newOrders = pending.Select(p => new { id = p.Id, date = p.Date, total = p.Total }) });
    }

    // Helpers: timeline file per order stored under App_Data/order_timeline/{id}.json
    private string TimelineFolder()
    {
        var folder = Path.Combine(_env.ContentRootPath, "App_Data", "order_timeline");
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        return folder;
    }

    private List<(string status, DateTime at)> ReadTimeline(string id)
    {
        try
        {
            var path = Path.Combine(TimelineFolder(), id + ".json");
            if (!System.IO.File.Exists(path))
            {
                // bootstrap with current status
                return new List<(string, DateTime)> { (GetOrderStatusSafe(id), DateTime.Now) };
            }
            var json = System.IO.File.ReadAllText(path);
            var items = JsonSerializer.Deserialize<List<TimelineItem>>(json) ?? new List<TimelineItem>();
            return items.Select(i => (i.Status, i.At)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read timeline for {id}", id);
            return new List<(string, DateTime)> { (GetOrderStatusSafe(id), DateTime.Now) };
        }
    }

    private string GetOrderStatusSafe(string id)
    {
        var o = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
        return o?.Status ?? "PENDING";
    }

    private void AppendTimeline(string id, string status)
    {
        try
        {
            var folder = TimelineFolder();
            var path = Path.Combine(folder, id + ".json");
            List<TimelineItem> items = new();
            if (System.IO.File.Exists(path))
            {
                var json = System.IO.File.ReadAllText(path);
                items = JsonSerializer.Deserialize<List<TimelineItem>>(json) ?? new List<TimelineItem>();
            }
            items.Add(new TimelineItem { Status = status, At = DateTime.Now });
            var outJson = JsonSerializer.Serialize(items);
            System.IO.File.WriteAllText(path, outJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append timeline for {id}", id);
        }
    }

    private class TimelineItem { public string Status { get; set; } public DateTime At { get; set; } }
}
