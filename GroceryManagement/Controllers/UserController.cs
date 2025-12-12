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
            //Email = u.Email,      // Read-only display
            PhoneNum = u.PhoneNum,
            Role = u.Role
        };

        // If it is a Staff member, fill in the extra details
        if (u is Staff s)
        {
            vm.Salary = s.Salary;
            vm.AuthorizationLvl = s.AuthorizationLvl;
            vm.ExistingPhotoURL = s.PhotoURL;
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

        if (ModelState.IsValid)
        {
            // Find the REAL record in the database using the ID from the form
            var dbUser = db.Users.Find(vm.Id);

            if (dbUser == null)
            {
                TempData["Info"] = "User not found.";
                return RedirectToAction("TestDBUsers");
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
                TempData["Error"] = "Cannot delete: This manager has staff assigned, and no other manager exists to take over.";
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

