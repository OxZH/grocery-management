using Microsoft.AspNetCore.Mvc;

namespace GroceryManagement.Controllers;

public class AttendanceController(DB db) : Controller
{
    public IActionResult CheckInAttendance()
    {
        return View();
    }
}

