using GroceryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroceryManagement.Controllers;

public class SalaryController(DB db, Helper hp) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public PayVM CalculateStaffSalary(Staff staff, List<AttendanceRecords> records, string currentManagerId, ref int nextSequence)
    {
        // Get today's date
        DateOnly today = DateOnly.FromDateTime(DateTime.Now);

        var vm = new PayVM
        {
            StaffId = staff.Id,
            StaffName = staff.Name,
            Role = staff.AuthorizationLvl,
            // Get default rate based on salary
            Salary = staff.Salary ?? 0,
        };

        foreach (var r in records)
        {
            double payableHours = 0;
            decimal pay = 0;
            decimal overtimeMultiplier = 1.5m; // 1.5x pay for extra hours
            string note = "";
            string checkOutDisplay = r.CheckOutTime?.ToString("HH:mm") ?? "Pending";

            // Convert dateonly to datetime to commpare
            DateTime expenseDate = r.Date.ToDateTime(TimeOnly.MinValue);
            
            var existingExpense = db.Expenses
                                    .FirstOrDefault(e => e.StaffId == staff.Id
                                                    && e.Date == expenseDate
                                                    && e.Type == "Salary");
            // If there is value in the db, use it
            if (existingExpense != null)
            {
                pay = existingExpense.Amount;

                // Calculate payable hours
                if (r.CheckInTime.HasValue && r.CheckOutTime.HasValue)
                {
                    payableHours = Math.Floor((r.CheckOutTime.Value - r.CheckInTime.Value).TotalHours);
                }

                vm.TotalSalary += pay;
                vm.TotalHours += payableHours;

                vm.DailyDetails.Add(new DailyPayDetailsVM
                {
                    Date = r.Date,
                    CheckIn = r.CheckInTime?.ToString("HH:mm") ?? "-",
                    CheckOut = checkOutDisplay,
                    HoursWorked = payableHours,
                    DailyPay = pay,
                    Note = "Paid (Locked)" 
                });
                continue; // Skip calc
            }

            // Calc new pay
            bool isReadyToSave = false;

            // If check in check out has value
            if (r.CheckInTime.HasValue && r.CheckOutTime.HasValue)
            {
                TimeSpan duration = r.CheckOutTime.Value - r.CheckInTime.Value;
                double rawTotalHours = duration.TotalHours;

                if (rawTotalHours <= 8)
                {
                    // Calculate pay
                    // Round down to nearest hour (7.9 -> 7)
                    payableHours = Math.Floor(rawTotalHours);
                    pay = (decimal)payableHours * vm.Salary;
                }
                else
                {
                    // Standard hour is always 8 hours
                    double standardHour = 8.0;
                    // Calculate raw extra time (e.g., 8.6 - 8 = 0.6)
                    double rawExtra = rawTotalHours - 8.0;
                    // Round Extra to nearest hour (0.4 -> 0, 0.5 -> 1)
                    double extraHour = Math.Round(rawExtra, MidpointRounding.AwayFromZero);

                    // Total hours 
                    payableHours = standardHour + extraHour;

                    // Calculate Pay components
                    decimal normalPay = (decimal)standardHour * vm.Salary;
                    decimal overtimePay = (decimal)extraHour * (vm.Salary * overtimeMultiplier);

                    pay = normalPay + overtimePay;
                }
                isReadyToSave = true;
            }

            // No checkout time 
            else if (r.CheckInTime.HasValue && r.CheckOutTime == null)
            {
                // If checkouttime is null but its on the same day
                if (r.Date == today)
                {
                    note = "Currently on Shift...";
                }
                // If the shift > 24 hours ago, and still no checkout, assume they forgot
                else if (r.Date < today && DateTime.Now > r.Date.ToDateTime(r.CheckInTime.Value).AddHours(24))
                {
                    // Calculate pay (half pay)
                    payableHours = 8; // Assume standard hrs
                    pay = (decimal)4 * vm.Salary;
                    note = "Missing Checkout (Half Pay).";
                    vm.MissingCheckouts++;
                    checkOutDisplay = "Missing";
                    isReadyToSave = true;
                }
            }

            if (isReadyToSave && pay > 0)
            {
                // Generate expense id
                string newId = "EX" + nextSequence.ToString("0000");
                nextSequence++;

                var expense = new Expense
                {
                    Id = newId,
                    Type = "Salary",
                    Details = $"Daily Pay (RM): {staff.Name} ({note})",
                    Date = expenseDate,
                    Amount = pay,
                    StaffId = staff.Id,
                    ManagerId = currentManagerId, 
                };
                // Add to db context
                db.Expenses.Add(expense);
            }
            // Add to VM total
            vm.TotalHours += payableHours;
            vm.TotalSalary += pay;

            // Add detail row
            vm.DailyDetails.Add(new DailyPayDetailsVM
            {
                Date = r.Date,
                CheckIn = r.CheckInTime?.ToString("HH:mm") ?? "-",
                CheckOut = checkOutDisplay,
                HoursWorked = payableHours,
                DailyPay = Math.Round(pay, 2),
                Note = note
            });
        }
        return vm;
    }

    // GET: Attendance/Pay
    [Authorize(Roles = "Manager")]
    public IActionResult Pay(int? month, int? year)
    {
        // Default to current month if not specified
        int m = month ?? DateTime.Now.Month;
        int y = year ?? DateTime.Now.Year;

        ViewBag.Month = m;
        ViewBag.Year = y;

        // 1. Get the Current Manager's ID
        string email = User.Identity?.Name;

        // Find the manager entity by email
        var currentManager = db.Managers.FirstOrDefault(u => u.Email == email);

        // Extract the ID (Safe navigation ?. in case manager is not found/seeding issue)
        string currentManagerId = currentManager?.Id ?? "Unknown";

        // Fetch Attendance Records
        var records = db.AttendanceRecords
                        .Where(a => a.Date.Month == m && a.Date.Year == y)
                        .Include(a => a.Staff)
                        .ToList();

        // 2. Fix the Variable Mismatch here
        // (Renamed 'expenseSequence' to 'runningSequence' to match your function call)
        int runningSequence = hp.GetNextExpenseSequence(db);

        var groupedRecords = records.GroupBy(a => a.Staff);
        var payList = new List<PayVM>();

        foreach (var group in groupedRecords)
        {
            // Now 'currentManagerId' and 'runningSequence' exist in this context
            var vm = CalculateStaffSalary(group.Key, group.ToList(), currentManagerId, ref runningSequence);
            payList.Add(vm);
        }

        // Save changes (Commits all new Expense records created inside the loop)
        db.SaveChanges();

        return View(payList);
    }

    // GET: Attendance/PayDetails
    [Authorize(Roles = "Manager")]
    public IActionResult PayDetails(string staffId, int month, int year)
    {
        var staff = db.Staffs.Find(staffId);
        if (staff == null) return NotFound();

        var records = db.AttendanceRecords
            .Where(a => a.StaffId == staffId && a.Date.Month == month && a.Date.Year == year)
            .OrderBy(a => a.Date)
            .ToList();

        int runningSequence = hp.GetNextExpenseSequence(db);

        // Lookup Manager ID 
        string email = User.Identity?.Name;
        var currentManager = db.Managers.FirstOrDefault(u => u.Email == email);
        string currentManagerId = currentManager?.Id ?? "Unknown";

        // Calculate (and queue up saves)
        var vm = CalculateStaffSalary(staff, records, currentManagerId, ref runningSequence);

        // Save
        db.SaveChanges();

        ViewBag.Month = month;
        ViewBag.Year = year;

        return View(vm);
    }
}
