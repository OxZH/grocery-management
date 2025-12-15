using GroceryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace GroceryManagement.Controllers;

public class AccountController(DB db, IWebHostEnvironment en, Helper hp) : Controller
{
    [Authorize(Roles = "Manager")]
    public IActionResult Index()
    {
        var users = db.Users.ToList();
        return View(users);
    }
    private string NextId(string prefix)
    {
        // only search for id with the specified prefix
        // if none found, assign {prefix}000
        var max = db.Users
            .Where(u => u.Id.StartsWith(prefix))
            .Max(u => u.Id) ?? $"{prefix}000";
        //take int part of the ID (T003 so take out 003), and the parse func trims off the leading 0s (so the 003 become 3)
        int n = int.Parse(max[1..]);
        //increment by 1 and format back to D3 (3 digits with leading 0s)
        return $"{prefix}{(n+1):D3}";
    }

    // GET: Account/Login
    public IActionResult Login()
    {
        return View();
    }

    // POST: Account/Login
    [HttpPost]
    public IActionResult Login(LoginVM vm, string? returnURL)
    {
        // (1) Get user (admin or member) record based on email
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        // (2) Custom validation -> verify password
        if (u == null || !hp.VerifyPassword(u.Password, vm.Password))
        {
            ModelState.AddModelError("", "Login credentials not matched.");
        }

        if (ModelState.IsValid)
        {
            TempData["Info"] = "Login successfully.";

            // (3) Sign in
            hp.SignIn(u!.Email, u.Role, vm.RememberMe, u.PhoneNum);

            // (4) Handle return URL
            if (string.IsNullOrEmpty(returnURL))
            {
                return RedirectToAction("Index", "Home");
            }
        }

        return View(vm);
    }

    // GET: Account/Logout
    public IActionResult Logout(string? returnURL)
    {
        TempData["Info"] = "Logout successfully.";

        // Sign out
        hp.SignOut();

        return RedirectToAction("Login", "Account");
    }

    // GET: Account/AccessDenied
    public IActionResult AccessDenied(string? returnURL)
    {
        return View();
    }

    // GET: Account/CheckEmail
    public bool CheckEmail(string email)
    {
        return !db.Users.Any(u => u.Email == email);
    }

    // GET: Account/Register
    [Authorize(Roles = "Manager")]
    public IActionResult Register()
    {
        return View();
    }

    // POST: Account/Register
    [HttpPost]
    [Authorize(Roles = "Manager")]
    public IActionResult Register(RegisterVM vm)
    {
        if (ModelState.IsValid("Email") &&
            db.Users.Any(u => u.Email == vm.Email))
        {
            ModelState.AddModelError("Email", "Duplicated Email.");
        }

        /*        if(vm.Photo != null)
                {
                    if (ModelState.IsValid("Photo"))
                    {   
                        var err = hp.ValidatePhoto(vm.Photo);
                        if (err != "") ModelState.AddModelError("Photo", err);
                    }

                }
                else
                {
                    ModelState.AddModelError("Photo", "Photo is required.");
                }*/
        if (vm.Role == "Manager")
        {
            ModelState.Remove("Salary");
            ModelState.Remove("AuthorizationLvl");
            ModelState.Remove("Photo");

            if (ModelState.IsValid)
            {
                var m = new Manager
                {
                    Id = NextId("M"),
                    Name = vm.Name,
                    Email = vm.Email,
                    Password = hp.HashPassword(vm.Password),
                    PhoneNum = vm.PhoneNum,
                    // Manager specific attributes
                };
                db.Managers.Add(m);
                db.SaveChanges();
                TempData["Info"] = $"Manager {m.Name} ({m.Id}) registered successfully.";
                return RedirectToAction("Index", "User");
            }
        }
        else
        {
            if (ModelState.IsValid)
            {
            
                // 1. Get the Email from the cookie
                string currentManagerEmail = User.Identity!.Name;
                // 2. Find the Manager object in the DB using that email
                var currentManager = db.Managers.FirstOrDefault(m => m.Email == currentManagerEmail);
                // Safety check (in case the manager was deleted but still has a cookie)
                if (currentManager == null) return RedirectToAction("Login", "Account");
                // save photos and keep the filename in a variable
                string unqiueFileName = hp.SavePhoto(vm.Photo, "photos");
                
                try
                {
                    // Insert staff object
                    var s = new Staff
                    {
                        Id = NextId("S"),
                        Name = vm.Name,
                        Email = vm.Email,
                        Password = hp.HashPassword(vm.Password),
                        PhoneNum = vm.PhoneNum,
                        // staff specific attributes
                        PhotoURL = unqiueFileName,
                        Salary = vm.Salary,
                        AuthorizationLvl = vm.AuthorizationLvl,
                        ManagerId = currentManager.Id,
                        //ManagerId = testManager,
                    };
                    // save to DB
                    db.Staffs.Add(s);
                    db.SaveChanges();

                    TempData["Info"] = $"Staff {s.Name} ({s.Id}) registered successfully.";
                    return RedirectToAction("Index", "User");
                }
                catch (Exception)
                {
                    hp.DeletePhoto(unqiueFileName, "photos");
                    ModelState.AddModelError("", "Error saving photo. Registration failed.");
                    return View(vm);
                }
            }
        }

      

        return View(vm);
    }

    // GET: Account/UpdatePassword
    [Authorize]
    public IActionResult UpdatePassword()
    {
        return View();
    }

    // POST: Account/UpdatePassword
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdatePassword(UpdatePasswordVM vm)
    {
        // Get user (admin or member) record based on email
        string userEmail = User.Identity!.Name!;
        var u = db.Users.FirstOrDefault(u => u.Email == userEmail);

        if (u == null)
        {
            hp.SignOut();
            return RedirectToAction("Login", "Account");
        }

        // If current password not matched
        if (!hp.VerifyPassword(u.Password, vm.Current))
        {
            ModelState.AddModelError("Current", "Incorrect Current Password.");
        }

        else if (hp.VerifyPassword(u.Password, vm.New))
        {
            ModelState.AddModelError("New", "New Password cannot be the same as Current Password.");
        }

        if (ModelState.IsValid)
        {
            // Update user password (hash)
            u.Password = hp.HashPassword(vm.New);
            db.SaveChanges();

            TempData["Info"] = "Password updated.";
            return RedirectToAction("Index", "Home");
        }

        return View(vm);
    }

    // GET: Account/UpdateProfile
    [Authorize]
    public IActionResult UpdateProfile()
    {
        // 1. Identify User
        string userEmail = User.Identity!.Name!;
        var u = db.Users.FirstOrDefault(u => u.Email == userEmail);

        if (u == null)
        {
            hp.SignOut();
            return RedirectToAction("Login");
        }

        // 2. Load Data into VM
        var vm = new UpdateProfileVM
        {
            Email = u.Email,
            Name = u.Name,
            PhoneNum = u.PhoneNum,
        };

        // 3. Load Photo (Only if user is Staff)
        if (u is Staff s)
        {
            vm.PhotoURL = s.PhotoURL;
        }

        return View(vm);
    }

    // POST: Account/UpdateProfile
    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(UpdateProfileVM vm)
    {
        // 1. Recover User
        string userEmail = User.Identity!.Name!;
        var u = db.Users.FirstOrDefault(u => u.Email == userEmail);

        if (u == null) return RedirectToAction("Login");

        // 2. Validate Photo (If one was uploaded)
        if (vm.Photo != null)
        {
            string err = hp.ValidatePhoto(vm.Photo);
            // if theres error add to modelstate
            if (err != "")
            {
                ModelState.AddModelError("Photo", err);
            }
        }

        if (ModelState.IsValid)
        {
            // 3. Update Basic Info
            u.Name = vm.Name;
            u.PhoneNum = vm.PhoneNum;

            // 4. Update Photo (Only for Staff)
            if (u is Staff s && vm.Photo != null)
            {
                // A. Delete old photo if exists
                if (!string.IsNullOrEmpty(s.PhotoURL))
                {
                    hp.DeletePhoto(s.PhotoURL, "photos");
                }

                // B. Save new photo
                s.PhotoURL = hp.SavePhoto(vm.Photo, "photos");
            }

            db.SaveChanges();
            TempData["Info"] = "Profile updated successfully.";

            // Optional: Refresh Cookie if your cookie stores Phone/Name
            // hp.SignIn(u.Email, u.Role, true, u.PhoneNum); 

            return RedirectToAction("UpdateProfile"); // Reload page to show changes
        }

        // If error, reload the existing photo URL so the image doesn't disappear
        if (u is Staff existingStaff)
        {
            vm.PhotoURL = existingStaff.PhotoURL;
        }

        // Also restore the email since it wasn't posted back
        vm.Email = u.Email;

        return View(vm);
    }

    // GET: Account/ResetPassword
    public IActionResult ResetPassword()
    {
        return View();
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordVM vm)
    {
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
        }

        if (ModelState.IsValid)
        {
            string password = hp.RandomPassword();

            u!.Password = hp.HashPassword(password);
            db.SaveChanges();

            // Send reset password email
            SendResetPasswordEmail(u, password);

            TempData["Info"] = $"Password reset. Check your email.";
            return RedirectToAction();
        }

        return View();
    }

    private void SendResetPasswordEmail(User u, string password)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Password";
        mail.IsBodyHtml = true;

        var url = Url.Action("Login", "User", null, "https");

        var path = u switch
        {
            Manager => Path.Combine(en.WebRootPath, "photos", "admin.jpg"),
            Staff s => Path.Combine(en.WebRootPath, "photos", s.PhotoURL),
            _ => "",
        };

        var att = new Attachment(path);
        mail.Attachments.Add(att);
        att.ContentId = "photo";

        mail.Body = $@"
            <img src='cid:photo' style='width: 200px; height: 200px;
                                        border: 1px solid #333'>
            <p>Dear {u.Name},<p>
            <p>Your password has been reset to:</p>
            <h1 style='color: red'>{password}</h1>
            <p>
                Please <a href='{url}'>login</a>
                with your new password.
            </p>
            <p>From, 🐱 Super Admin</p>
        ";

        hp.SendEmail(mail);
    }
}
