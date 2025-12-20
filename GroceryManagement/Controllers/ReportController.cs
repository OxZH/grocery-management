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
        DateTime startDateTime = DateTime.Now.AddDays(-30);
        DateTime endDateTime = DateTime.Now;
        var proc = db.ProcurementRecords
            .Where(p => p.ProcurementDateTime > startDateTime
                && p.ProcurementDateTime < endDateTime)
            .ToList();

        ViewBag.Products = db.Products.ToList();
        ViewBag.Suppliers = db.Suppliers.ToList();
        return View(proc);
    }
}
