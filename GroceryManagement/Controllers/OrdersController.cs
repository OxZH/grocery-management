using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GroceryManagement.Controllers;

public class OrdersController : Controller
{
    private readonly DB _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<OrdersController> _logger;
    private static readonly string[] Workflow = new[] { "PENDING", "PICKING", "PACKED", "READY", "COMPLETED", "CANCELLED" };

    public OrdersController(DB db, IWebHostEnvironment env, ILogger<OrdersController> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    // GET: /Orders
    public IActionResult Index(string search, string statusFilter, string sortBy = "Date", string sortDir = "desc")
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

        var list = q.Take(200).ToList(); // keep result set bounded
        return View(list);
    }

    // GET: /Orders/Details/{id}
    public IActionResult Details(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var order = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
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

    // GET: /Orders/PackingSlip/{id}
    public IActionResult PackingSlip(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var order = _db.Set<Checkout>().FirstOrDefault(c => c.Id == id);
        if (order == null) return NotFound();
        return View(order);
    }

    // GET: /Orders/Notifications
    public IActionResult Notifications(DateTime? since)
    {
        var q = _db.Set<Checkout>().AsQueryable();
        if (since.HasValue)
        {
            q = q.Where(c => c.Date > since.Value);
        }
        var pending = q.Where(c => c.Status == "PENDING").OrderByDescending(c => c.Date).Take(10).ToList();
        return Json(new { newOrders = pending.Select(p => new { p.Id, p.Date, p.Total }) });
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
