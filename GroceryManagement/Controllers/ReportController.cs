using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GroceryManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroceryManagement.Controllers;

public class ReportController(DB db) : Controller
{
    public IActionResult Index()
    {
        
        return View();
    }

    [HttpPost]
    public IActionResult Index(DateRangeVM vm)
    {
        DateTime startDateTime = vm.StartDate.ToDateTime(TimeOnly.MinValue);
        DateTime endDateTime = vm.EndDate.ToDateTime(TimeOnly.MaxValue);

        ViewBag.Procurements = db.ProcurementRecords
            .Where(p => p.ProcurementDateTime > startDateTime
                && p.ProcurementDateTime < endDateTime)
            .ToList();
        ViewBag.Products = db.Products.ToList();
        ViewBag.Suppliers = db.Suppliers.ToList();

        return View(vm);
    }
}
