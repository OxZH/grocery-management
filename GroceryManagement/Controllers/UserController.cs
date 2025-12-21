using GroceryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;
using X.PagedList.Extensions;
using X.PagedList.Mvc.Core;

namespace GroceryManagement.Controllers;

public class UserController(DB db,
                            IWebHostEnvironment en,
                            Helper hp) : Controller
{
    // GET: User/Index
    public IActionResult Index(string? name, string? sort, string? dir, int page = 1)
    {
        // (1) Searching ------------------------
        // Store search term to View to keep it in the input box
        ViewBag.Name = name = name?.Trim() ?? "";

        var query = db.Users.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            query = query.Where(u => u.Name.Contains(name) || u.Email.Contains(name));
        }

        // (2) Sorting --------------------------
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        // Map string "sort" to the actual property
        Func<User, object> fn = sort switch
        {
            "Id" => u => u.Id,
            "Name" => u => u.Name,
            "Email" => u => u.Email,
            "Role" => u => u.Role,
            // Handle Salary sorting safely (users who aren't staff get 0/null)
            "Salary" => u => (u is Staff s) ? s.Salary : 0,
            _ => u => u.Id
        };

        // Apply sorting (Note: Func<User,object> forces client-side evaluation for these complex types)
        var sorted = dir == "des" ?
                     query.OrderByDescending(fn).AsQueryable() :
                     query.OrderBy(fn).AsQueryable();

        // (3) Paging ---------------------------
        int pageSize = 5;

        // Validate page number
        if (page < 1) return RedirectToAction(null, new { name, sort, dir, page = 1 });

        // Create Paged List
        var model = sorted.ToPagedList(page, pageSize);

        // Redirect if page is out of bounds
        if (page > model.PageCount && model.PageCount > 0)
        {
            return RedirectToAction(null, new { name, sort, dir, page = model.PageCount });
        }

        // (4) AJAX Response --------------------
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest") // Standard AJAX check
        {
            return PartialView("_StaffTable", model);
        }

        return View(model);
    }

    // GET: Home/Detail
    public IActionResult Detail(string? id)
    {
        var model = db.Users.Find(id);

        if (model == null)
        {
            TempData["Info"] = "User not found";
            return RedirectToAction("Index");
        }

        return View(model);
    }

    // GET: Home/CheckId 
    public bool CheckId(string id)
    {
        return !db.Users.Any(s => s.Id == id);
    }


    // Unblock staff
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public IActionResult Unblock(string id)
    {
        var staff = db.Users.Find(id);
        if (staff != null)
        {
            staff.Locked = null;
            staff.LoginAttempts = 0;
            db.SaveChanges();
            TempData["Info"] = $"{staff.Name} unblocked.";
        }

        return RedirectToAction("Index", "User");
    }

    // GET
    [Authorize(Roles = "Manager")]
    public IActionResult Update(string? id)
    {
        var u = db.Users.Find(id);

        if (u == null)
        {
            TempData["Info"] = "User not found.";
            return RedirectToAction("Index");
        }

        // Map Database Object -> ViewModel
        var vm = new UserUpdateVM
        {
            Id = u.Id,
            Name = u.Name,
            PhoneNum = u.PhoneNum,
            Role = u.Role
        };

        // If it is a Staff member, fill in the extra details
        if (u is Staff s)
        {
            vm.Salary = s.Salary;
            vm.AuthorizationLvl = s.AuthorizationLvl;
            vm.ExistingPhotoURL = s.PhotoURL;
            vm.ManagerId = s.ManagerId;
            // Get list of managers for dropdown
            var managers = db.Managers.OrderBy(m => m.Name).ToList();
            // Create SelectList for the ViewModel
            vm.ManagerList = new SelectList(managers, "Id", "Name", s.ManagerId);
        }

        return View(vm);
    }

    // POST: 
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public IActionResult Update(UserUpdateVM vm)
    {
        // Remove role validation;just for display in vm
        ModelState.Remove("Role");
        // Remove ManagerList validation; just for dropdown population
        ModelState.Remove("ManagerList");
        // Remove ExistingPhotoURL validation; just for display
        ModelState.Remove("ExistingPhotoURL");

        // If manager, remove unnecessary validations
        if (vm.Role == "Manager")
        {
            ModelState.Remove("Salary");
            ModelState.Remove("AuthorizationLvl");
            ModelState.Remove("ManagerId");
        }
        // If staff, salary cannot be empty
        else
        {
            if (vm.Salary == null)
            {
                ModelState.AddModelError("Salary", "Salary cannot be empty");
            }

        }
        if (ModelState.IsValid)
        {
            // Find the record in the database using the ID from the form
            var dbUser = db.Users.Find(vm.Id);

            if (dbUser == null)
            {
                TempData["Info"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Update common fields (name, phone)
            dbUser.Name = vm.Name;
            dbUser.PhoneNum = vm.PhoneNum;

            // Update staff specifics
            if (dbUser is Staff dbStaff)
            {
                // Update if the form provided a value
                if (vm.Salary != null)
                {
                    dbStaff.Salary = vm.Salary;
                    // Additional CHECK: ManagerId exists in Managers table
                    if (vm.ManagerId != null && db.Managers.Any(m => m.Id == vm.ManagerId))
                    {
                        dbStaff.ManagerId = vm.ManagerId;
                    }
                }

                if (!string.IsNullOrEmpty(vm.AuthorizationLvl))
                {
                    dbStaff.AuthorizationLvl = vm.AuthorizationLvl;
                }

                // Photo handling
                if (vm.Photo != null)
                {
                    // validate the new photo
                    var err = hp.ValidatePhoto(vm.Photo);
                    if (err != "")
                    {
                        ModelState.AddModelError("Photo", err);
                        return View(vm);
                    }
                    // Attempt to save new photo to make sure no errors
                    string? newPhoto = hp.SavePhoto(vm.Photo, "images/users");

                    if (string.IsNullOrEmpty(newPhoto))
                    {
                        ModelState.AddModelError("Photo", "The image could not be saved. It may be corrupted.");
                        return View(vm);
                    }

                    // Delete the old photo
                    if (!string.IsNullOrEmpty(dbStaff.PhotoURL))
                    {
                        hp.DeletePhoto(dbStaff.PhotoURL, "images/users");
                    }

                    // C. Save new photo
                    dbStaff.PhotoURL = newPhoto;
                }
            }

            // 5. Save Changes
            db.SaveChanges();
            TempData["Info"] = $"User {dbUser.Name} ({dbUser.Id}) updated successfully.";

            return RedirectToAction("Index");
        }

        /*if (db.Users.Find(vm.Id) is Staff)
        {
            vm.ManagerList = new SelectList(db.Managers.OrderBy(m => m.Name), "Id", "Name", vm.ManagerId);
        }*/

        // RELOAD DROPDOWN (Crucial so page doesn't break on error)
        if (vm.Role == "Staff" || vm.Role == null)
        {
            vm.ManagerList = new SelectList(db.Managers.OrderBy(m => m.Name), "Id", "Name", vm.ManagerId);
        }

        return View(vm);
    }

    // POST: Account/Delete
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public IActionResult Delete(string? id)
    {
        // 1. Find the user
        var u = db.Users.Find(id);

        // Safe Referer Logic (Prevents crashes if Referer is null)
        string refererUrl = Request.Headers.Referer.ToString() ?? "/";

        if (u == null)
        {
            TempData["Info"] = "User not found.";
            return Redirect(refererUrl);
        }

        // Capture identity for self deletion check
        bool isSelfDeletion = (u.Email == User.Identity?.Name);

        // Prepare photo path 
        string? photoToDelete = (u is Staff s) ? s.PhotoURL : null;

        // 2. Reassign staff if manager deleted
        if (u is Manager)
        {
            var subordinates = db.Staffs.Where(s => s.ManagerId == u.Id).ToList();
            if (subordinates.Count > 0)
            {
                // Find other managers 
                var otherManager = db.Managers
                    .Where(m => m.Id != u.Id && m.Id.StartsWith("M"))
                    .FirstOrDefault();

                // No managers to take over
                if (otherManager == null)
                {
                    TempData["Info"] = "Cannot delete: This manager has staff, and no other manager exists to take over.";
                    return Redirect(refererUrl);
                }

                // Reasssign all staff to new manager
                foreach (var staff in subordinates)
                {
                    staff.ManagerId = otherManager.Id;
                }
            }
        }
        // 3. Try user deletion
        try
        {
            db.Users.Remove(u);

            // Attempts to commit the delete (error if there are dependencies) 
            db.SaveChanges();

            // If no dependencies 
            if (!string.IsNullOrEmpty(photoToDelete))
            {
                // Delete physical file
                hp.DeletePhoto(photoToDelete, "images/users");
            }

            TempData["Info"] = "Record deleted successfully.";
        }
        // If there are dependencies
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Reload the user (because the failed delete might have messed up the tracking state)
            db.Entry(u).State = Microsoft.EntityFrameworkCore.EntityState.Unchanged;

            TempData["Info"] = "Delete Failed: This user cannot be deleted because they have existing records in the system (e.g., Attendance, Sales, or Inventory).";

            return Redirect(refererUrl);
        }

        // 4. Logout if self deleted
        if (isSelfDeletion)
        {
            hp.SignOut();
            return RedirectToAction("Login", "Account");
        }

        return Redirect(refererUrl);
    }
}

