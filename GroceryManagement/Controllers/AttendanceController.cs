using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.IO;

namespace GroceryManagement.Controllers;

public class AttendanceController(DB db, IWebHostEnvironment env) : Controller
{
    private string? GetCurrentUserId()
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email)) return null;
        
        return db.Users.FirstOrDefault(u => u.Email == email)?.Id;
    }
    [Authorize(Roles = "Staff")]
    public IActionResult CheckInAttendance(string? overrideDate = null, string? overrideTime = null)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction("Index", "Home");
        }

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
        ViewBag.StaffId = staffId;
        ViewBag.HasCheckIn = false;
        ViewBag.HasCheckout = false;
        ViewBag.CanCheckout = false;

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

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff")]
    public IActionResult CheckIn(string? overrideDate = null, string? overrideTime = null)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction(nameof(CheckInAttendance));
        }

        var checkDate = string.IsNullOrWhiteSpace(overrideDate)
            ? DateOnly.FromDateTime(DateTime.Now)
            : DateOnly.Parse(overrideDate);

        var checkTime = string.IsNullOrWhiteSpace(overrideTime)
            ? TimeOnly.FromDateTime(DateTime.Now)
            : TimeOnly.Parse(overrideTime);

        // Rule: Check-in must be after 8:00 AM
        var minimumCheckInTime = new TimeOnly(8, 0);
        if (checkTime < minimumCheckInTime)
        {
            TempData["Info"] = $"<p class='error'>Check-in is only allowed after 8:00 AM. Your attempted time: {checkTime}.</p>";
            return RedirectToAction(nameof(CheckInAttendance), new
            {
                overrideDate = checkDate.ToString("yyyy-MM-dd"),
                overrideTime = checkTime.ToString("HH:mm")
            });
        }

        var alreadyCheckedIn = db.AttendanceRecords
            .FirstOrDefault(a => a.StaffId == staffId && a.Date == checkDate);

        if (alreadyCheckedIn != null)
        {
            TempData["Info"] = $"<p class='error'>You already checked in at {alreadyCheckedIn.CheckInTime} on {alreadyCheckedIn.Date:yyyy-MM-dd}.</p>";
            return RedirectToAction(nameof(CheckInAttendance), new
            {
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
        TempData["ShowCheckInNotification"] = "true";

        return RedirectToAction(nameof(CheckInAttendance), new
        {
            overrideDate = checkDate.ToString("yyyy-MM-dd"),
            overrideTime = checkTime.ToString("HH:mm")
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff")]
    public IActionResult CheckOut(string? overrideDate = null, string? overrideTime = null)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
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
                overrideDate = checkDate.ToString("yyyy-MM-dd"),
                overrideTime = checkTime.ToString("HH:mm")
            });
        }

        // Rule: Check-out must be after check-in time
        if (checkTime <= record.CheckInTime)
        {
            TempData["Info"] = $"<p class='error'>Check-out time ({checkTime}) must be after check-in time ({record.CheckInTime}).</p>";
            return RedirectToAction(nameof(CheckInAttendance), new
            {
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
        
        TempData["ShowCheckOutNotification"] = "true";

        return RedirectToAction(nameof(CheckInAttendance), new
        {
            overrideDate = checkDate.ToString("yyyy-MM-dd"),
            overrideTime = checkTime.ToString("HH:mm")
        });
    }

    [Authorize(Roles = "Staff")]
    public IActionResult AttendanceHistory(string? search = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction("Index", "Home");
        }

        var query = db.AttendanceRecords
            .Where(a => a.StaffId == staffId)
            .AsQueryable();

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            query = query.Where(a => a.Status == status);
        }

        // Search by date
        if (!string.IsNullOrWhiteSpace(search))
        {
            if (DateOnly.TryParse(search, out var searchDate))
            {
                query = query.Where(a => a.Date == searchDate);
            }
        }

        var totalRecords = query.Count();
        var records = query
            .OrderByDescending(a => a.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        ViewBag.Search = search;
        ViewBag.Status = status ?? "ALL";
        ViewBag.CurrentPage = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalRecords = totalRecords;
        ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        ViewBag.StaffId = staffId;

        return View(records);
    }

    [Authorize(Roles = "Staff")]
    public IActionResult ApplyLeave()
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction("Index", "Home");
        }

        var vm = new LeaveApplyVM
        {
            Form = new LeaveRequestFormVM
            {
                Type = "ADVANCE",
                LeaveDate = DateOnly.FromDateTime(DateTime.Now.AddDays(1))
            },
            Requests = db.LeaveRequests
                .Where(l => l.StaffId == staffId)
                .OrderByDescending(l => l.SubmittedAt)
                .Take(50)
                .ToList()
        };

        ViewBag.StaffId = staffId;
        return View("ApplyLeave", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff")]
    public IActionResult ApplyLeave(LeaveApplyVM vm, IFormFile? attachment)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction(nameof(ApplyLeave));
        }

        if (vm?.Form is null)
        {
            TempData["Info"] = "<p class='error'>Invalid form submission.</p>";
            return RedirectToAction(nameof(ApplyLeave));
        }

        var leaveDate = vm.Form.LeaveDate;
        var type = vm.Form.Type?.ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.Now);

        if (type is null || (type != "ADVANCE" && type != "MC"))
        {
            ModelState.AddModelError("Form.Type", "Type must be ADVANCE or MC.");
        }
        else
        {
            vm.Form.Type = type;
        }

        if (type == "ADVANCE" && leaveDate <= today)
        {
            ModelState.AddModelError("Form.LeaveDate", "Advance leave must be submitted before the leave date.");
        }

        if (type == "MC" && attachment is null)
        {
            ModelState.AddModelError("", "Medical leave requires an attachment (image or PDF).");
        }

        string? relativePath = null;
        if (attachment is not null)
        {
            if (!IsAllowedFile(attachment))
            {
                ModelState.AddModelError("", "Only PNG, JPG, JPEG, or PDF up to 2MB are allowed.");
            }
            else
            {
                relativePath = SaveAttachment(attachment);
            }
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join(" | ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Info"] = $"<p class='error'>Validation failed: {errors}</p>";
            
            vm.Requests = db.LeaveRequests
                .Where(l => l.StaffId == staffId)
                .OrderByDescending(l => l.SubmittedAt)
                .Take(50)
                .ToList();
            ViewBag.StaffId = staffId;
            return View("ApplyLeave", vm);
        }

        var leave = new LeaveRequest
        {
            Id = GenerateLeaveId(),
            StaffId = staffId,
            LeaveDate = leaveDate,
            Type = type,
            Status = "PENDING",
            Reason = vm.Form.Reason,
            AttachmentPath = relativePath,
            SubmittedAt = DateTime.Now
        };

        db.LeaveRequests.Add(leave);
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Leave application {leave.Id} submitted for approval. Type: {leave.Type}, Date: {leave.LeaveDate}</p>";
        return RedirectToAction(nameof(ApplyLeave));
    }

    [Authorize(Roles = "Manager")]
    public IActionResult LeaveApprovals()
    {
        var managerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(managerId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current manager.</p>";
            return RedirectToAction("Index", "Home");
        }
        
        var requests = db.LeaveRequests
            .OrderBy(r => r.Status == "PENDING" ? 0 : 1)
            .ThenByDescending(r => r.SubmittedAt)
            .Take(200)
            .ToList();

        ViewBag.ManagerId = managerId;
        
        // AJAX Support
        if (Request.IsAjax())
        {
            return PartialView("_LeaveApprovalsTable", requests);
        }
        
        return View("LeaveApprovals", requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult ApproveLeave(string id)
    {
        var managerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(managerId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current manager.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(LeaveApprovals));
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        var req = db.LeaveRequests.FirstOrDefault(r => r.Id == id);
        if (req is null)
        {
            TempData["Info"] = "<p class='error'>Leave request not found.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(LeaveApprovals));
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        req.Status = "APPROVED";
        req.ApprovedBy = managerId;
        req.ApprovedAt = DateTime.Now;
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Approved {req.Id}.</p>";
        
        // AJAX Support - return updated table
        if (Request.IsAjax())
        {
            return RedirectToAction(nameof(LeaveApprovals));
        }
        
        return RedirectToAction(nameof(LeaveApprovals));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult RejectLeave(string id)
    {
        var managerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(managerId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current manager.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(LeaveApprovals));
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        var req = db.LeaveRequests.FirstOrDefault(r => r.Id == id);
        if (req is null)
        {
            TempData["Info"] = "<p class='error'>Leave request not found.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(LeaveApprovals));
            }
            return RedirectToAction(nameof(LeaveApprovals));
        }

        req.Status = "REJECTED";
        req.ApprovedBy = managerId;
        req.ApprovedAt = DateTime.Now;
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Rejected {req.Id}.</p>";
        
        // AJAX Support - return updated table
        if (Request.IsAjax())
        {
            return RedirectToAction(nameof(LeaveApprovals));
        }
        
        return RedirectToAction(nameof(LeaveApprovals));
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

    private string GenerateLeaveId()
    {
        var lastId = db.LeaveRequests
            .OrderByDescending(a => a.Id)
            .Select(a => a.Id)
            .FirstOrDefault();

        var nextNumber = 1;
        if (!string.IsNullOrWhiteSpace(lastId) && lastId.Length > 2)
        {
            var numericPart = lastId.Substring(2);
            if (int.TryParse(numericPart, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"LR{nextNumber:D5}";
    }

    private bool IsAllowedFile(IFormFile file)
    {
        var allowedTypes = new[] { "image/png", "image/jpeg", "application/pdf" };
        var maxSize = 2 * 1024 * 1024;
        return file.Length > 0 && file.Length <= maxSize && allowedTypes.Contains(file.ContentType);
    }

    private string SaveAttachment(IFormFile file)
    {
        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "leave");
        Directory.CreateDirectory(uploadsDir);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(uploadsDir, fileName);

        using var stream = System.IO.File.Create(physicalPath);
        file.CopyTo(stream);

        return $"/uploads/leave/{fileName}";
    }
}