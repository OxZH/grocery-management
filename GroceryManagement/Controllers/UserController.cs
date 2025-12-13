using GroceryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GroceryManagement.Controllers;

public class UserController (DB db, 
                            IWebHostEnvironment en, Helper hp) : Controller
{
    // GET: Home/Index
    public IActionResult Index()
    {
        var model = db.Users;
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

    /*    // GET: Home/CheckProgramId
        public bool CheckProgramId(string programId)
        {
            return db.Programs.Any(p => p.Id == programId);
        }*/

    // GET
    [Authorize(Roles ="Manager")]
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
    [Authorize(Roles ="Manager")]
    public IActionResult Update(UserUpdateVM vm)
    {
        // Remove role validation;just for display in vm
        ModelState.Remove("Role");
        // Remove ManagerList validation; just for dropdown population
        ModelState.Remove("ManagerList");

        if (ModelState.IsValid)
        {
            // Find the REAL record in the database using the ID from the form
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

                    // Delete the old photo
                    if (!string.IsNullOrEmpty(dbStaff.PhotoURL))
                    {
                        hp.DeletePhoto(dbStaff.PhotoURL, "photos");
                    }

                    // C. Save new photo
                    dbStaff.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
                }
            }

            // 5. Save Changes
            db.SaveChanges();
            TempData["Info"] = $"User {dbUser.Name} ({dbUser.Id}) updated successfully.";

            return RedirectToAction("Index");
        }

        if(db.Users.Find(vm.Id) is Staff)
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
        // 1. Find the user to be deleted
        var u = db.Users.Find(id);

        if (u == null)
        {
            TempData["Info"] = "User not found.";
            return Redirect(Request.Headers.Referer.ToString());
        }

        // 2. CHECK: Is this a Manager?
        // We check if any staff report to this user (u.Id)
        var subordinates = db.Staffs.Where(s => s.ManagerId == u.Id).ToList();

        if (subordinates.Count > 0)
        {
            // 3. REASSIGNMENT LOGIC
            // Find another manager who is NOT the one we are deleting
            var otherManager = db.Managers
                .Where(m => m.Id != u.Id && m.Id.StartsWith("M"))
                .FirstOrDefault();

            if (otherManager == null)
            {
                // Scenario: No one else to take over. Block deletion.
                TempData["Info"] = "Cannot delete: This manager has staff assigned, and no other manager exists to take over.";
                return Redirect(Request.Headers.Referer.ToString());
            }

            // 4. Reassign all staff to the new manager
            foreach (var staff in subordinates)
            {
                staff.ManagerId = otherManager.Id;
            }

            // Save the reassignment BEFORE deleting the old manager
            db.SaveChanges();
            TempData["Info"] = $"Staff reassigned to {otherManager.Name}. ";
        }

        // 5. STANDARD DELETE LOGIC (Now safe to proceed)

        // Check if there is a photo to clean up (only for Staff)
        string? photoToDelete = null;
        if (u is Staff staffMember)
        {
            photoToDelete = staffMember.PhotoURL;
        }

        db.Users.Remove(u);
        db.SaveChanges();

        // User.Identity.Name holds the Email (based on your Helper.SignIn logic)
        if (u.Email == User.Identity!.Name)
        {
            // 1. Kill the cookie immediately
            hp.SignOut();

            TempData["Info"] = "Your account has been deleted.";
            // 2. Kick them back to Login page (not the previous page, because they have no access)
            return RedirectToAction("Login", "Account");
        }

        // Delete physical file
        if (!string.IsNullOrEmpty(photoToDelete))
        {
            hp.DeletePhoto(photoToDelete, "photos");
        }

        TempData["Info"] += "Record deleted successfully.";

        return Redirect(Request.Headers.Referer.ToString());
    }

    /*    // POST: Home/DeleteMany
        [HttpPost]
        public IActionResult DeleteMany(string[] ids)
        {
            int n = db.Users
                .Where(s => ids.Contains(s.Id))
                .ExecuteDelete();

            TempData["Info"] = $"{n} record(s) deleted.";
            return RedirectToAction("Demo");
        }*/
    /*
        // POST: Home/Restore
        [HttpPost]
        public IActionResult Restore()
        {
            // (1) Delete all records
            db.Users.ExecuteDelete();

            // ------------------------------------------------

            // (2) Insert all records from "Users.txt"
            string path = Path.Combine(en.ContentRootPath, "Users.txt");

            foreach (string line in System.IO.File.ReadLines(path))
            {
                if (line.Trim() == "") continue;

                var data = line.Split("\t", StringSplitOptions.TrimEntries);

                db.Users.Add(new()
                {
                    Id = data[0],
                    Name = data[1],
                    Gender = data[2],
                    ProgramId = data[3],
                });
            }

            db.SaveChanges();

            TempData["Info"] = "Record(s) restored.";
            return RedirectToAction("Demo");
        }*/
}

