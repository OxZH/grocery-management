using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

namespace GroceryManagement.Controllers;

public class AttendanceController(DB db) : Controller
{
    public IActionResult CheckInAttendance(string? staffId = null, string? overrideDate = null, string? overrideTime = null)
    {
        LoadStaffDropdown(staffId);

        var currentDate = string.IsNullOrWhiteSpace(overrideDate)
            ? DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd")
            : overrideDate;
        var currentTime = string.IsNullOrWhiteSpace(overrideTime)
            ? TimeOnly.FromDateTime(DateTime.Now).ToString("HH:mm")
            : overrideTime;

        ViewBag.CurrentDate = currentDate;
        ViewBag.CurrentTime = currentTime;
        ViewBag.OverrideDate = overrideDate;
        ViewBag.OverrideTime = overrideTime;
        ViewBag.SelectedStaffId = staffId;
        ViewBag.HasCheckIn = false;
        ViewBag.HasCheckout = false;
        ViewBag.CanCheckout = false;

        if (!string.IsNullOrWhiteSpace(staffId))
        {
            var parsedDate = DateOnly.Parse(currentDate);
            var existingRecord = db.AttendanceRecords
                .FirstOrDefault(a => a.StaffId == staffId && a.Date == parsedDate);

            if (existingRecord != null)
            {
                ViewBag.HasCheckIn = true;
                ViewBag.AlreadyCheckedIn = true;
                ViewBag.CheckInInfo = $"Checked in on {existingRecord.Date:yyyy-MM-dd} at {existingRecord.CheckInTime}";
                ViewBag.HasCheckout = existingRecord.CheckOutTime != null;
                ViewBag.CanCheckout = true; // allow rewriting checkout time
                if (existingRecord.CheckOutTime is not null)
                {
                    ViewBag.CheckOutInfo = $"Checked out at {existingRecord.CheckOutTime}";
                }
            }
            else
            {
                ViewBag.AlreadyCheckedIn = false;
                ViewBag.CheckInInfo = "No check-in found for this date.";
            }
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckIn(string staffId, string? overrideDate = null, string? overrideTime = null)
    {
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Please select a staff member before checking in.</p>";
            return RedirectToAction(nameof(CheckInAttendance));
        }

        var checkDate = string.IsNullOrWhiteSpace(overrideDate)
            ? DateOnly.FromDateTime(DateTime.Now)
            : DateOnly.Parse(overrideDate);

        var checkTime = string.IsNullOrWhiteSpace(overrideTime)
            ? TimeOnly.FromDateTime(DateTime.Now)
            : TimeOnly.Parse(overrideTime);

        var alreadyCheckedIn = db.AttendanceRecords
            .FirstOrDefault(a => a.StaffId == staffId && a.Date == checkDate);

        if (alreadyCheckedIn != null)
        {
            TempData["Info"] = $"<p class='error'>You already checked in at {alreadyCheckedIn.CheckInTime} on {alreadyCheckedIn.Date:yyyy-MM-dd}.</p>";
            return RedirectToAction(nameof(CheckInAttendance), new
            {
                staffId,
                overrideDate = checkDate.ToString("yyyy-MM-dd"),
                overrideTime = checkTime.ToString("HH:mm")
            });
        }

        var attendanceRecord = new AttendanceRecords
        {
            Id = GenerateAttendanceId(),
            StaffId = staffId,
            Date = checkDate,
            CheckInTime = checkTime,
            Status = "ATTEND"
        };

        db.AttendanceRecords.Add(attendanceRecord);
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Checked in at {checkTime} on {checkDate:yyyy-MM-dd}.</p>";

        return RedirectToAction(nameof(CheckInAttendance), new
        {
            staffId,
            overrideDate = checkDate.ToString("yyyy-MM-dd"),
            overrideTime = checkTime.ToString("HH:mm")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckOut(string staffId, string? overrideDate = null, string? overrideTime = null)
    {
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Please select a staff member before checking out.</p>";
            return RedirectToAction(nameof(CheckInAttendance));
        }

        var checkDate = string.IsNullOrWhiteSpace(overrideDate)
            ? DateOnly.FromDateTime(DateTime.Now)
            : DateOnly.Parse(overrideDate);

        var checkTime = string.IsNullOrWhiteSpace(overrideTime)
            ? TimeOnly.FromDateTime(DateTime.Now)
            : TimeOnly.Parse(overrideTime);

        var record = db.AttendanceRecords
            .FirstOrDefault(a => a.StaffId == staffId && a.Date == checkDate);

        if (record is null)
        {
            TempData["Info"] = $"<p class='error'>No check-in record found for {checkDate:yyyy-MM-dd}. Cannot check out.</p>";
            return RedirectToAction(nameof(CheckInAttendance), new
            {
                staffId,
                overrideDate = checkDate.ToString("yyyy-MM-dd"),
                overrideTime = checkTime.ToString("HH:mm")
            });
        }

        var priorCheckout = record.CheckOutTime;
        record.CheckOutTime = checkTime;
        db.SaveChanges();

        if (priorCheckout is not null)
        {
            TempData["Info"] = $"<p class='success'>Checkout time updated to {checkTime} (was {priorCheckout}) on {checkDate:yyyy-MM-dd}.</p>";
        }
        else
        {
            TempData["Info"] = $"<p class='success'>Checked out at {checkTime} on {checkDate:yyyy-MM-dd}.</p>";
        }

        return RedirectToAction(nameof(CheckInAttendance), new
        {
            staffId,
            overrideDate = checkDate.ToString("yyyy-MM-dd"),
            overrideTime = checkTime.ToString("HH:mm")
        });
    }

    private void LoadStaffDropdown(string? selectedStaffId)
    {
        var staffs = db.Staffs
            .Select(s => new
            {
                s.Id,
                Name = $"{s.Id} - {s.Name}"
            })
            .ToList();

        ViewBag.Staffs = new SelectList(staffs, "Id", "Name", selectedStaffId);
    }

    private string GenerateAttendanceId()
    {
        var lastId = db.AttendanceRecords
            .OrderByDescending(a => a.Id)
            .Select(a => a.Id)
            .FirstOrDefault();

        var nextNumber = 1;
        if (!string.IsNullOrWhiteSpace(lastId) && lastId.Length > 3)
        {
            var numericPart = lastId.Substring(3);
            if (int.TryParse(numericPart, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"ATT{nextNumber:D5}";
    }
}