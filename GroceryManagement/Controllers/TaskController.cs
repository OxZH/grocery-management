using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GroceryManagement.Controllers;

[Authorize]
public class TaskController(DB db) : Controller
{
    private string? GetCurrentUserId()
    {
        var email = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email)) return null;
        return db.Users.FirstOrDefault(u => u.Email == email)?.Id;
    }

    // ===== MANAGER ACTIONS: TASK TYPES =====

    [Authorize(Roles = "Manager")]
    public IActionResult TaskTypes()
    {
        var taskTypes = db.TaskTypes.OrderBy(t => t.Name).ToList();
        
        // AJAX Support
        if (Request.IsAjax())
        {
            return PartialView("_TaskTypesTable", taskTypes);
        }
        
        return View(taskTypes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult AddTaskType(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Info"] = "<p class='error'>Task type name is required.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(TaskTypes));
            }
            return RedirectToAction(nameof(TaskTypes));
        }

        if (db.TaskTypes.Any(t => t.Name == name.Trim()))
        {
            TempData["Info"] = "<p class='error'>Task type already exists.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(TaskTypes));
            }
            return RedirectToAction(nameof(TaskTypes));
        }

        var taskType = new TaskType
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        db.TaskTypes.Add(taskType);
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Task type '{name}' added successfully.</p>";
        
        // AJAX Support - return updated table
        if (Request.IsAjax())
        {
            return RedirectToAction(nameof(TaskTypes));
        }
        
        return RedirectToAction(nameof(TaskTypes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult ToggleTaskType(int id)
    {
        var taskType = db.TaskTypes.FirstOrDefault(t => t.Id == id);
        if (taskType == null)
        {
            TempData["Info"] = "<p class='error'>Task type not found.</p>";
            
            if (Request.IsAjax())
            {
                return RedirectToAction(nameof(TaskTypes));
            }
            return RedirectToAction(nameof(TaskTypes));
        }

        taskType.IsActive = !taskType.IsActive;
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Task type '{taskType.Name}' {(taskType.IsActive ? "activated" : "deactivated")}.</p>";
        
        // AJAX Support - return updated table
        if (Request.IsAjax())
        {
            return RedirectToAction(nameof(TaskTypes));
        }
        
        return RedirectToAction(nameof(TaskTypes));
    }

    // ===== MANAGER ACTIONS: ROSTER TEMPLATES =====

    [Authorize(Roles = "Manager")]
    public IActionResult Templates()
    {
        var templates = db.RosterTemplates
            .Include(t => t.Allocations)
            .ThenInclude(a => a.Staff)
            .OrderBy(t => t.TemplateName)
            .ToList();
        return View(templates);
    }

    [Authorize(Roles = "Manager")]
    public IActionResult CreateTemplate()
    {
        LoadStaffDropdown();
        LoadTaskTypeDropdown();
        return View(new RosterTemplateFormVM());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult CreateTemplate(RosterTemplateFormVM vm)
    {
        var managerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(managerId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current manager.</p>";
            return RedirectToAction(nameof(Templates));
        }

        if (!ModelState.IsValid || vm.Allocations == null || !vm.Allocations.Any())
        {
            LoadStaffDropdown();
            TempData["Info"] = "<p class='error'>Please add at least one staff-task assignment.</p>";
            return View(vm);
        }

        // Check for duplicate template name
        if (db.RosterTemplates.Any(t => t.TemplateName == vm.TemplateName))
        {
            LoadStaffDropdown();
            ModelState.AddModelError("TemplateName", "A template with this name already exists.");
            return View(vm);
        }

        // Check for duplicate staff in same template
        var staffIds = vm.Allocations.Select(a => a.StaffId).ToList();
        if (staffIds.Count != staffIds.Distinct().Count())
        {
            LoadStaffDropdown();
            TempData["Info"] = "<p class='error'>Each staff member can only be assigned once per template.</p>";
            return View(vm);
        }

        var template = new RosterTemplate
        {
            Id = GenerateRosterTemplateId(),
            TemplateName = vm.TemplateName,
            ManagerId = managerId,
            CreatedAt = DateTime.Now
        };

        db.RosterTemplates.Add(template);

        // Add template allocations
        for (int i = 0; i < vm.Allocations.Count; i++)
        {
            var alloc = vm.Allocations[i];
            var templateAlloc = new TemplateAllocation
            {
                TemplateId = template.Id,
                StaffId = alloc.StaffId,
                TaskName = alloc.TaskName,
                SortOrder = i
            };
            db.TemplateAllocations.Add(templateAlloc);
        }

        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Roster template '{template.TemplateName}' created successfully.</p>";
        return RedirectToAction(nameof(Templates));
    }

    [Authorize(Roles = "Manager")]
    public IActionResult EditTemplate(string id)
    {
        var template = db.RosterTemplates
            .Include(t => t.Allocations)
            .ThenInclude(a => a.Staff)
            .FirstOrDefault(t => t.Id == id);

        if (template == null)
        {
            TempData["Info"] = "<p class='error'>Template not found.</p>";
            return RedirectToAction(nameof(Templates));
        }

        var vm = new RosterTemplateFormVM
        {
            Id = template.Id,
            TemplateName = template.TemplateName,
            Allocations = template.Allocations.OrderBy(a => a.SortOrder).Select(a => new TemplateAllocationVM
            {
                Id = a.Id,
                StaffId = a.StaffId,
                StaffName = a.Staff.Name,
                TaskName = a.TaskName,
                SortOrder = a.SortOrder
            }).ToList()
        };

        LoadStaffDropdown();
        LoadTaskTypeDropdown();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult EditTemplate(RosterTemplateFormVM vm)
    {
        if (!ModelState.IsValid || vm.Allocations == null || !vm.Allocations.Any())
        {
            LoadStaffDropdown();
            TempData["Info"] = "<p class='error'>Please add at least one staff-task assignment.</p>";
            return View(vm);
        }

        var template = db.RosterTemplates
            .Include(t => t.Allocations)
            .FirstOrDefault(t => t.Id == vm.Id);

        if (template == null)
        {
            TempData["Info"] = "<p class='error'>Template not found.</p>";
            return RedirectToAction(nameof(Templates));
        }

        // Check for duplicate template name (excluding current)
        if (db.RosterTemplates.Any(t => t.TemplateName == vm.TemplateName && t.Id != vm.Id))
        {
            LoadStaffDropdown();
            ModelState.AddModelError("TemplateName", "A template with this name already exists.");
            return View(vm);
        }

        // Check for duplicate staff
        var staffIds = vm.Allocations.Select(a => a.StaffId).ToList();
        if (staffIds.Count != staffIds.Distinct().Count())
        {
            LoadStaffDropdown();
            TempData["Info"] = "<p class='error'>Each staff member can only be assigned once per template.</p>";
            return View(vm);
        }

        template.TemplateName = vm.TemplateName;

        // Remove old allocations
        db.TemplateAllocations.RemoveRange(template.Allocations);

        // Add new allocations
        for (int i = 0; i < vm.Allocations.Count; i++)
        {
            var alloc = vm.Allocations[i];
            var templateAlloc = new TemplateAllocation
            {
                TemplateId = template.Id,
                StaffId = alloc.StaffId,
                TaskName = alloc.TaskName,
                SortOrder = i
            };
            db.TemplateAllocations.Add(templateAlloc);
        }

        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Template '{template.TemplateName}' updated successfully.</p>";
        return RedirectToAction(nameof(Templates));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult DeleteTemplate(string id)
    {
        var template = db.RosterTemplates.FirstOrDefault(t => t.Id == id);
        if (template == null)
        {
            TempData["Info"] = "<p class='error'>Template not found.</p>";
            return RedirectToAction(nameof(Templates));
        }

        // Check if template is used in any day schedule
        var isUsed = db.DaySchedules.Any(ds => ds.TemplateId == id);
        if (isUsed)
        {
            TempData["Info"] = "<p class='error'>Cannot delete template that has been applied to schedules.</p>";
            return RedirectToAction(nameof(Templates));
        }

        db.RosterTemplates.Remove(template);
        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Template deleted successfully.</p>";
        return RedirectToAction(nameof(Templates));
    }

    // ===== MANAGER ACTIONS: CALENDAR & SCHEDULING =====

    [Authorize(Roles = "Manager")]
    public IActionResult Calendar(int? year, int? month)
    {
        var today = DateTime.Now;
        var calendarYear = year ?? today.Year;
        var calendarMonth = month ?? today.Month;

        var firstDayOfMonth = new DateOnly(calendarYear, calendarMonth, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Get day schedules for this month
        var schedules = db.DaySchedules
            .Include(ds => ds.Template)
            .Where(ds => ds.ScheduleDate >= firstDayOfMonth && ds.ScheduleDate <= lastDayOfMonth)
            .ToDictionary(ds => ds.ScheduleDate);

        // Get attendance for the month to determine status
        var attendanceRecords = db.AttendanceRecords
            .Where(a => a.Date >= firstDayOfMonth && a.Date <= lastDayOfMonth)
            .GroupBy(a => a.Date)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.StaffId, a => a.Status));

        var days = new List<CalendarDayVM>();

        // Add leading days from previous month
        int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        if (firstDayOfWeek > 0)
        {
            var prevMonthStart = firstDayOfMonth.AddDays(-firstDayOfWeek);
            for (int i = 0; i < firstDayOfWeek; i++)
            {
                days.Add(new CalendarDayVM
                {
                    Date = prevMonthStart.AddDays(i),
                    DayNumber = prevMonthStart.AddDays(i).Day,
                    IsCurrentMonth = false
                });
            }
        }

        // Add current month days
        for (int day = 1; day <= lastDayOfMonth.Day; day++)
        {
            var date = new DateOnly(calendarYear, calendarMonth, day);
            var dayVM = new CalendarDayVM
            {
                Date = date,
                DayNumber = day,
                IsCurrentMonth = true
            };

            if (schedules.TryGetValue(date, out var schedule))
            {
                dayVM.HasSchedule = true;
                dayVM.TemplateName = schedule.Template.TemplateName;

                // Determine status
                if (schedule.IsAcknowledged && schedule.HasUnavailableStaff)
                {
                    dayVM.Status = "ACKNOWLEDGED";
                }
                else if (schedule.HasUnavailableStaff)
                {
                    dayVM.Status = "WARNING";
                }
                else if (attendanceRecords.ContainsKey(date))
                {
                    // Check if all staff in allocations are present
                    var allocations = db.Allocations.Where(a => a.AssignedDate == date).ToList();
                    var allPresent = allocations.All(a =>
                        attendanceRecords[date].ContainsKey(a.StaffId) &&
                        attendanceRecords[date][a.StaffId] == "ATTEND"
                    );
                    dayVM.Status = allPresent ? "OK" : "WARNING";
                }
                else
                {
                    dayVM.Status = "OK"; // Future date or no attendance yet
                }
            }

            days.Add(dayVM);
        }

        // Add trailing days from next month
        int lastDayOfWeek = (int)lastDayOfMonth.DayOfWeek;
        if (lastDayOfWeek < 6)
        {
            var nextMonthStart = lastDayOfMonth.AddDays(1);
            for (int i = 0; i <= 6 - lastDayOfWeek - 1; i++)
            {
                days.Add(new CalendarDayVM
                {
                    Date = nextMonthStart.AddDays(i),
                    DayNumber = nextMonthStart.AddDays(i).Day,
                    IsCurrentMonth = false
                });
            }
        }

        var vm = new CalendarMonthVM
        {
            Year = calendarYear,
            Month = calendarMonth,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(calendarMonth),
            Days = days
        };

        return View(vm);
    }

    [Authorize(Roles = "Manager")]
    public IActionResult ApplyTemplate(string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        // Prevent applying template to past dates
        if (date < DateOnly.FromDateTime(DateTime.Now))
        {
            TempData["Info"] = "<p class='error'>Cannot apply template to past dates.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        // Check if already scheduled
        if (db.DaySchedules.Any(ds => ds.ScheduleDate == date))
        {
            TempData["Info"] = "<p class='error'>This date already has a schedule. View day details to modify.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        var templates = db.RosterTemplates
            .OrderBy(t => t.TemplateName)
            .Select(t => new { t.Id, t.TemplateName })
            .ToList();

        var vm = new ApplyTemplateVM
        {
            Date = date,
            Templates = new SelectList(templates, "Id", "TemplateName")
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult ApplyTemplate(ApplyTemplateVM vm)
    {
        var managerId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(managerId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current manager.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(vm.SelectedTemplateId))
        {
            TempData["Info"] = "<p class='error'>Please select a template.</p>";
            return RedirectToAction(nameof(ApplyTemplate), new { dateStr = vm.Date.ToString("yyyy-MM-dd") });
        }

        // Check if already scheduled
        if (db.DaySchedules.Any(ds => ds.ScheduleDate == vm.Date))
        {
            TempData["Info"] = "<p class='error'>This date already has a schedule.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        var template = db.RosterTemplates
            .Include(t => t.Allocations)
            .FirstOrDefault(t => t.Id == vm.SelectedTemplateId);

        if (template == null)
        {
            TempData["Info"] = "<p class='error'>Template not found.</p>";
            return RedirectToAction(nameof(ApplyTemplate), new { dateStr = vm.Date.ToString("yyyy-MM-dd") });
        }

        // Create day schedule
        var daySchedule = new DaySchedule
        {
            ScheduleDate = vm.Date,
            TemplateId = template.Id,
            AppliedBy = managerId,
            AppliedAt = DateTime.Now
        };

        db.DaySchedules.Add(daySchedule);

        // Create allocations from template
        var startingId = GetNextAllocationNumber();
        var counter = 0;
        foreach (var templateAlloc in template.Allocations)
        {
            var allocation = new Allocation
            {
                Id = $"ALC{(startingId + counter):D7}",
                TemplateId = template.Id,
                StaffId = templateAlloc.StaffId,
                TaskName = templateAlloc.TaskName,
                AssignedDate = vm.Date,
                Status = "PENDING"
            };
            db.Allocations.Add(allocation);
            counter++;
        }

        // Check attendance to set HasUnavailableStaff flag
        var attendance = GetStaffAttendance(vm.Date);
        var staffIds = template.Allocations.Select(a => a.StaffId).ToList();
        daySchedule.HasUnavailableStaff = staffIds.Any(sid =>
            attendance.ContainsKey(sid) && attendance[sid] != "ATTEND"
        );

        db.SaveChanges();

        if (daySchedule.HasUnavailableStaff)
        {
            TempData["Info"] = "<p class='warning'>Template applied, but some staff are unavailable. Please review and reassign.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr = vm.Date.ToString("yyyy-MM-dd") });
        }
        else
        {
            TempData["Info"] = "<p class='success'>Template applied successfully. All staff are available.</p>";
            return RedirectToAction(nameof(Calendar), new { year = vm.Date.Year, month = vm.Date.Month });
        }
    }

    // NEW: Day Management with Tabs (Apply Template + Attendance List + Schedule Details)
    [Authorize(Roles = "Manager")]
    public IActionResult DayManagement(string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        // Check if day has schedule
        var hasSchedule = db.DaySchedules.Any(ds => ds.ScheduleDate == date);

        // Get templates for Apply Template tab
        var templates = db.RosterTemplates
            .Include(t => t.Allocations)
            .OrderBy(t => t.TemplateName)
            .ToList();

        // Get all staff with their attendance status for that day
        var allStaff = db.Staffs.OrderBy(s => s.Name).ToList();
        var attendanceRecords = db.AttendanceRecords
            .Where(a => a.Date == date)
            .ToDictionary(a => a.StaffId, a => a.Status);

        var staffList = allStaff.Select(s => new StaffAttendanceVM
        {
            StaffId = s.Id,
            StaffName = s.Name,
            PhotoURL = s.PhotoURL,
            AttendanceStatus = attendanceRecords.ContainsKey(s.Id) ? attendanceRecords[s.Id] : "UNKNOWN"
        }).ToList();

        var vm = new DayManagementVM
        {
            Date = date,
            HasSchedule = hasSchedule,
            Templates = templates,
            StaffList = staffList
        };

        return View(vm);
    }

    [Authorize(Roles = "Manager")]
    public IActionResult DayDetails(string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        var daySchedule = db.DaySchedules
            .Include(ds => ds.Template)
            .FirstOrDefault(ds => ds.ScheduleDate == date);

        if (daySchedule == null)
        {
            TempData["Info"] = "<p class='error'>No schedule found for this date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        var allocations = db.Allocations
            .Include(a => a.Staff)
            .Where(a => a.AssignedDate == date)
            .ToList();

        var attendance = GetStaffAttendance(date);
        var availableStaff = GetAvailableStaff(date);

        var vm = new DayDetailsVM
        {
            Date = date,
            TemplateName = daySchedule.Template.TemplateName,
            HasUnavailableStaff = daySchedule.HasUnavailableStaff,
            IsAcknowledged = daySchedule.IsAcknowledged,
            Allocations = allocations.Select(a => new DayAllocationVM
            {
                AllocationId = a.Id,
                StaffId = a.StaffId,
                StaffName = a.Staff.Name,
                TaskName = a.TaskName,
                AttendanceStatus = attendance.GetValueOrDefault(a.StaffId, "UNKNOWN"),
                IsUnavailable = attendance.GetValueOrDefault(a.StaffId, "UNKNOWN") != "ATTEND",
                Status = a.Status,
                StartTime = a.StartTime,
                CompletionDate = a.CompletionDate,
                Notes = a.Notes
            }).OrderBy(a => a.TaskName).ToList(),
            AvailableStaffForReassignment = availableStaff
        };

        LoadTaskTypeDropdown();
        return View(vm);
    }

    // Add Staff to Day Schedule
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult AddStaffToDay(string staffId, string taskName, string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        if (string.IsNullOrWhiteSpace(staffId) || string.IsNullOrWhiteSpace(taskName))
        {
            TempData["Info"] = "<p class='error'>Please select both staff and task.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        // Check if staff already assigned on this day
        var existingAllocation = db.Allocations
            .FirstOrDefault(a => a.StaffId == staffId && a.AssignedDate == date);

        if (existingAllocation != null)
        {
            TempData["Info"] = "<p class='error'>This staff member is already assigned on this day.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        // Create new allocation
        var newAllocation = new Allocation
        {
            Id = GenerateAllocationId(),
            StaffId = staffId,
            TaskName = taskName,
            AssignedDate = date,
            Status = "PENDING"
        };

        db.Allocations.Add(newAllocation);
        
        // Update day schedule status
        var daySchedule = db.DaySchedules.FirstOrDefault(ds => ds.ScheduleDate == date);
        if (daySchedule != null)
        {
            var attendance = GetStaffAttendance(date);
            var allAllocations = db.Allocations.Where(a => a.AssignedDate == date).ToList();
            allAllocations.Add(newAllocation);
            daySchedule.HasUnavailableStaff = allAllocations.Any(a =>
                attendance.GetValueOrDefault(a.StaffId, "UNKNOWN") != "ATTEND"
            );
        }

        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Staff added to schedule successfully.</p>";
        return RedirectToAction(nameof(DayDetails), new { dateStr });
    }

    // Delete Staff from Day Schedule
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult DeleteStaffAllocation(string allocationId, string dateStr)
    {
        var allocation = db.Allocations.FirstOrDefault(a => a.Id == allocationId);
        
        if (allocation == null)
        {
            TempData["Info"] = "<p class='error'>Allocation not found.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        var staffName = db.Staffs.FirstOrDefault(s => s.Id == allocation.StaffId)?.Name ?? allocation.StaffId;
        
        db.Allocations.Remove(allocation);
        
        // Update day schedule status
        if (DateOnly.TryParse(dateStr, out var date))
        {
            var daySchedule = db.DaySchedules.FirstOrDefault(ds => ds.ScheduleDate == date);
            if (daySchedule != null)
            {
                var attendance = GetStaffAttendance(date);
                var allAllocations = db.Allocations.Where(a => a.AssignedDate == date).ToList();
                daySchedule.HasUnavailableStaff = allAllocations.Any(a =>
                    attendance.GetValueOrDefault(a.StaffId, "UNKNOWN") != "ATTEND"
                );
            }
        }

        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>{staffName} removed from schedule.</p>";
        return RedirectToAction(nameof(DayDetails), new { dateStr });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult EditTaskName(string allocationId, string newTaskName, string dateStr)
    {
        if (string.IsNullOrWhiteSpace(newTaskName))
        {
            TempData["Info"] = "<p class='error'>Task name cannot be empty.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        var allocation = db.Allocations.FirstOrDefault(a => a.Id == allocationId);
        if (allocation == null)
        {
            TempData["Info"] = "<p class='error'>Allocation not found.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        var oldTaskName = allocation.TaskName;
        allocation.TaskName = newTaskName.Trim();
        
        db.SaveChanges();

        TempData["Info"] = $"<p class='success'>Task updated from '{oldTaskName}' to '{newTaskName}'.</p>";
        return RedirectToAction(nameof(DayDetails), new { dateStr });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult ReassignTask(string allocationId, string newStaffId, string dateStr)
    {
        if (string.IsNullOrWhiteSpace(newStaffId))
        {
            TempData["Info"] = "<p class='error'>Please select a staff member.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        var allocation = db.Allocations.FirstOrDefault(a => a.Id == allocationId);
        if (allocation == null)
        {
            TempData["Info"] = "<p class='error'>Allocation not found.</p>";
            return RedirectToAction(nameof(DayDetails), new { dateStr });
        }

        var oldStaffId = allocation.StaffId;
        allocation.StaffId = newStaffId;
        allocation.Status = "PENDING"; // Reset status
        allocation.StartTime = null;
        allocation.CompletionDate = null;
        allocation.Notes = (allocation.Notes ?? "") + $"\n[Reassigned from {oldStaffId} on {DateTime.Now:yyyy-MM-dd HH:mm}]";

        // Update day schedule flag
        var daySchedule = db.DaySchedules.FirstOrDefault(ds => ds.ScheduleDate == allocation.AssignedDate);
        if (daySchedule != null)
        {
            var attendance = GetStaffAttendance(allocation.AssignedDate);
            var allAllocations = db.Allocations.Where(a => a.AssignedDate == allocation.AssignedDate).ToList();
            daySchedule.HasUnavailableStaff = allAllocations.Any(a =>
                attendance.GetValueOrDefault(a.StaffId, "UNKNOWN") != "ATTEND"
            );
        }

        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Staff reassigned successfully.</p>";
        return RedirectToAction(nameof(DayDetails), new { dateStr });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult AcknowledgeDay(string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        var daySchedule = db.DaySchedules.FirstOrDefault(ds => ds.ScheduleDate == date);
        if (daySchedule == null)
        {
            TempData["Info"] = "<p class='error'>Schedule not found.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        daySchedule.IsAcknowledged = true;
        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Day acknowledged. Staff unavailability will be kept as is.</p>";
        return RedirectToAction(nameof(DayDetails), new { dateStr });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public IActionResult DeleteSchedule(string dateStr)
    {
        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(Calendar));
        }

        var daySchedule = db.DaySchedules.FirstOrDefault(ds => ds.ScheduleDate == date);
        if (daySchedule != null)
        {
            db.DaySchedules.Remove(daySchedule);
        }

        var allocations = db.Allocations.Where(a => a.AssignedDate == date).ToList();
        db.Allocations.RemoveRange(allocations);

        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Schedule deleted successfully.</p>";
        return RedirectToAction(nameof(Calendar), new { year = date.Year, month = date.Month });
    }

    // ===== STAFF ACTIONS =====

    [Authorize(Roles = "Staff")]
    public IActionResult MySchedule(int? year, int? month)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction("Index", "Home");
        }

        var staff = db.Staffs.FirstOrDefault(s => s.Id == staffId);
        if (staff == null)
        {
            TempData["Info"] = "<p class='error'>Staff not found.</p>";
            return RedirectToAction("Index", "Home");
        }

        var today = DateTime.Now;
        var calendarYear = year ?? today.Year;
        var calendarMonth = month ?? today.Month;

        var firstDayOfMonth = new DateOnly(calendarYear, calendarMonth, 1);
        var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

        // Get my allocations for the month
        var myAllocations = db.Allocations
            .Where(a => a.StaffId == staffId &&
                        a.AssignedDate >= firstDayOfMonth &&
                        a.AssignedDate <= lastDayOfMonth)
            .ToDictionary(a => a.AssignedDate);

        var days = new List<MyCalendarDayVM>();

        // Add leading days
        int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        if (firstDayOfWeek > 0)
        {
            var prevMonthStart = firstDayOfMonth.AddDays(-firstDayOfWeek);
            for (int i = 0; i < firstDayOfWeek; i++)
            {
                days.Add(new MyCalendarDayVM
                {
                    Date = prevMonthStart.AddDays(i),
                    DayNumber = prevMonthStart.AddDays(i).Day,
                    IsCurrentMonth = false
                });
            }
        }

        // Add current month days
        for (int day = 1; day <= lastDayOfMonth.Day; day++)
        {
            var date = new DateOnly(calendarYear, calendarMonth, day);
            var dayVM = new MyCalendarDayVM
            {
                Date = date,
                DayNumber = day,
                IsCurrentMonth = true
            };

            if (myAllocations.TryGetValue(date, out var allocation))
            {
                dayVM.HasAssignment = true;
                dayVM.TaskName = allocation.TaskName;
            }

            days.Add(dayVM);
        }

        // Add trailing days
        int lastDayOfWeek = (int)lastDayOfMonth.DayOfWeek;
        if (lastDayOfWeek < 6)
        {
            var nextMonthStart = lastDayOfMonth.AddDays(1);
            for (int i = 0; i <= 6 - lastDayOfWeek - 1; i++)
            {
                days.Add(new MyCalendarDayVM
                {
                    Date = nextMonthStart.AddDays(i),
                    DayNumber = nextMonthStart.AddDays(i).Day,
                    IsCurrentMonth = false
                });
            }
        }

        var vm = new MyScheduleVM
        {
            Year = calendarYear,
            Month = calendarMonth,
            MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(calendarMonth),
            Days = days,
            StaffId = staffId,
            StaffName = staff.Name
        };

        return View(vm);
    }

    [Authorize(Roles = "Staff")]
    public IActionResult MyDayTask(string dateStr)
    {
        var staffId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(staffId))
        {
            TempData["Info"] = "<p class='error'>Unable to identify current user.</p>";
            return RedirectToAction(nameof(MySchedule));
        }

        if (!DateOnly.TryParse(dateStr, out var date))
        {
            TempData["Info"] = "<p class='error'>Invalid date.</p>";
            return RedirectToAction(nameof(MySchedule));
        }

        var allocation = db.Allocations
            .FirstOrDefault(a => a.StaffId == staffId && a.AssignedDate == date);

        if (allocation == null)
        {
            TempData["Info"] = "<p class='error'>No task assigned for this date.</p>";
            return RedirectToAction(nameof(MySchedule));
        }

        // Get teammates on same task
        var teammates = GetTeammates(date, allocation.TaskName, staffId);

        var vm = new MyDayTaskVM
        {
            Date = date,
            AllocationId = allocation.Id,
            TaskName = allocation.TaskName,
            Status = allocation.Status,
            StartTime = allocation.StartTime,
            CompletionDate = allocation.CompletionDate,
            Notes = allocation.Notes,
            Teammates = teammates
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff")]
    public IActionResult StartTask(string id, string dateStr)
    {
        var staffId = GetCurrentUserId();
        var allocation = db.Allocations.FirstOrDefault(a => a.Id == id && a.StaffId == staffId);

        if (allocation == null)
        {
            TempData["Info"] = "<p class='error'>Task not found or not assigned to you.</p>";
            return RedirectToAction(nameof(MySchedule));
        }

        if (allocation.Status != "PENDING")
        {
            TempData["Info"] = "<p class='error'>Task is not in pending status.</p>";
            return RedirectToAction(nameof(MyDayTask), new { dateStr });
        }

        allocation.Status = "IN_PROGRESS";
        allocation.StartTime = DateTime.Now;
        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Task started.</p>";
        return RedirectToAction(nameof(MyDayTask), new { dateStr });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Staff")]
    public IActionResult CompleteTask(string id, string? notes, string dateStr)
    {
        var staffId = GetCurrentUserId();
        var allocation = db.Allocations.FirstOrDefault(a => a.Id == id && a.StaffId == staffId);

        if (allocation == null)
        {
            TempData["Info"] = "<p class='error'>Task not found or not assigned to you.</p>";
            return RedirectToAction(nameof(MySchedule));
        }

        if (allocation.Status == "COMPLETED")
        {
            TempData["Info"] = "<p class='error'>Task is already completed.</p>";
            return RedirectToAction(nameof(MyDayTask), new { dateStr });
        }

        allocation.Status = "COMPLETED";
        allocation.CompletionDate = DateTime.Now;
        if (allocation.StartTime == null)
        {
            allocation.StartTime = DateTime.Now;
        }
        if (!string.IsNullOrWhiteSpace(notes))
        {
            allocation.Notes = notes;
        }

        db.SaveChanges();

        TempData["Info"] = "<p class='success'>Task marked as completed.</p>";
        return RedirectToAction(nameof(MyDayTask), new { dateStr });
    }

    // ===== HELPER METHODS =====

    private string GenerateRosterTemplateId()
    {
        var lastId = db.RosterTemplates
            .OrderByDescending(t => t.Id)
            .Select(t => t.Id)
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

        return $"RST{nextNumber:D5}";
    }

    private string GenerateAllocationId()
    {
        var nextNumber = GetNextAllocationNumber();
        return $"ALC{nextNumber:D7}";
    }

    private int GetNextAllocationNumber()
    {
        var lastId = db.Allocations
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

        return nextNumber;
    }

    private Dictionary<string, string> GetStaffAttendance(DateOnly date)
    {
        return db.AttendanceRecords
            .Where(a => a.Date == date)
            .ToDictionary(a => a.StaffId, a => a.Status);
    }

    private List<Staff> GetAvailableStaff(DateOnly date)
    {
        var attendance = GetStaffAttendance(date);
        return db.Staffs
            .AsEnumerable()
            .Where(s => attendance.GetValueOrDefault(s.Id, "UNKNOWN") == "ATTEND")
            .ToList();
    }

    private List<TeammateVM> GetTeammates(DateOnly date, string taskName, string excludeStaffId)
    {
        var teammates = db.Allocations
            .Include(a => a.Staff)
            .Where(a => a.AssignedDate == date && a.TaskName == taskName && a.StaffId != excludeStaffId)
            .ToList();

        var attendance = GetStaffAttendance(date);

        return teammates.Select(a => new TeammateVM
        {
            StaffId = a.StaffId,
            StaffName = a.Staff.Name,
            AttendanceStatus = attendance.GetValueOrDefault(a.StaffId, "UNKNOWN")
        }).ToList();
    }

    private void LoadStaffDropdown(string? selectedStaffId = null)
    {
        var staff = db.Staffs
            .OrderBy(s => s.Name)
            .Select(s => new { s.Id, Display = $"{s.Name} ({s.Id}) - {s.AuthorizationLvl}" })
            .ToList();
        ViewBag.Staffs = new SelectList(staff, "Id", "Display", selectedStaffId);
    }

    private void LoadTaskTypeDropdown(string? selectedTaskType = null)
    {
        var taskTypes = db.TaskTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => t.Name)
            .ToList();
        ViewBag.TaskTypes = new SelectList(taskTypes);
    }
}
