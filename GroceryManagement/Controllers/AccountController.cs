using GroceryManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace GroceryManagement.Controllers;

public class AccountController(DB db, IWebHostEnvironment en, Helper hp) : Controller
{
/*    [Authorize(Roles = "Manager")]
    public IActionResult Index()
    {
        var users = db.Users.ToList();
        return View(users);
    }*/
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
        // (1) Get user record based on email
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        // User not found
        if (u == null)
        {
            ModelState.AddModelError("", "Login credentials not matched.");
            vm.Password = string.Empty; // Remove previously entered password 
            return View(vm); // Reload current page with entered data
        }

        // (2) Check Locked
        if (u.Locked != null && u.Locked > DateTime.Now)
        {
            if (u is Manager)
            {
                TimeSpan remaining = u.Locked.Value - DateTime.Now;
                ModelState.AddModelError("", $"Account locked. Try again in {(int)remaining.Seconds} seconds.");            }
            else
            {
                ModelState.AddModelError("", $"Account locked due to multiple failed attempts. Please contact a Manager.");
            }
            return View(vm);
        }
        // (3) Verify password
        if (!hp.VerifyPassword(u.Password, vm.Password))
        {
            u.LoginAttempts++;

            if (u.LoginAttempts >= 3)
            {
                if (u is Manager)
                {
                    u.Locked = DateTime.Now.AddMinutes(1); // Set to 1 minute for testing
                }
                else
                {
                    u.Locked = DateTime.MaxValue; // Lock indefinitely
                }
                db.SaveChanges();
                ModelState.AddModelError("", "Maximum attempts reached. Account Locked.");
                return View(vm);
            }
            // Normal fail ( < 3 attempts)
            int remaining = 3-u.LoginAttempts;
            db.SaveChanges(); // Save incremented count

            ModelState.AddModelError("", $"Invalid credentials. You have {remaining} attempts left.");
            return View(vm);
        }

        if (ModelState.IsValid)
        {
            // Reset if success
            u.LoginAttempts = 0;
            u.Locked = null;
            db.SaveChanges();

            TempData["Info"] = "Login successfully.";

            // Sign in
            hp.SignIn(u!.Email, u.Role, vm.RememberMe, u.PhoneNum);

            // Handle return URL
            if (string.IsNullOrEmpty(returnURL))
            {
                return RedirectToAction("Index", "Home");
            }
            else if (Url.IsLocalUrl(returnURL))
            {
                return Redirect(returnURL);
            }
            else
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

        // Validate Photo
        if (vm.Photo != null)
        {
            string err = hp.ValidatePhoto(vm.Photo);
            // if theres error add to modelstate
            if (err != "")
            {
                ModelState.AddModelError("Photo", err);
            }
        }

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
                string unqiueFileName = hp.SavePhoto(vm.Photo, "images/users");
                
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
                    hp.DeletePhoto(unqiueFileName, "images/users");
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
                    hp.DeletePhoto(s.PhotoURL, "images/users");
                }

                // B. Save new photo
                s.PhotoURL = hp.SavePhoto(vm.Photo, "images/users");
            }

            db.SaveChanges();
            TempData["Info"] = "Profile updated successfully.";

            // Optional: Refresh Cookie 
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

    // GET: Account/ForgotPassword
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: Account/ForgotPassword
    [HttpPost]
    public IActionResult ForgotPassword(UpdatePasswordVM vm)
    {
        // 1. Find user
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        // Security Note: Generally, you shouldn't reveal if an email exists, 
        // but for school projects, checking specific errors is fine.
        if (u == null)
        {
            ModelState.AddModelError("Email", "Email not found.");
            return View(vm);
        }

        // 2. Generate Token & Expiry (e.g., 30 mins)
        string token = Guid.NewGuid().ToString();
        u.ResetToken = token;
        u.ResetTokenExpiry = DateTime.Now.AddMinutes(30);
        db.SaveChanges();

        // 3. Send Email with Link
        SendResetPasswordEmail(u, token);

        TempData["Info"] = "Reset link sent! Check your email.";
        return RedirectToAction("Login", "Account");
    }

    // GET: Account/ResetPassword?token=...&email=...
    // [HttpGet]
    public IActionResult ResetPassword(string token, string email)
    {
        // 1. Basic Validation
        if (token == null || email == null)
        {
            TempData["Info"] = "Invalid password reset token.";
            return RedirectToAction("Login", "Account");
        }

        // 2. Pass data to the View so the form can submit it back
        var vm = new UpdatePasswordVM
        {
            Token = token,
            Email = email
        };

        return View(vm);
    }

    // POST: Account/ResetPassword
    [HttpPost]
    public IActionResult ResetPassword(UpdatePasswordVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        // 3. Find User & Verify Token
        var u = db.Users.FirstOrDefault(u => u.Email == vm.Email);

        if (u == null ||
            u.ResetToken != vm.Token ||
            u.ResetTokenExpiry < DateTime.Now)
        {
            TempData["Info"] = "Token is invalid or has expired.";
            return RedirectToAction("ForgotPassword");
        }

        // 4. Update Password & Clear Token
        u.Password = hp.HashPassword(vm.New);
        u.ResetToken = null;       // Invalidate token so it can't be used again
        u.ResetTokenExpiry = null;

        db.SaveChanges();

        TempData["Info"] = "Password updated successfully. Please login.";
        return RedirectToAction("Login", "Account");
    }

    private void SendResetPasswordEmail(User u, string token)
    {
        var mail = new MailMessage();
        mail.To.Add(new MailAddress(u.Email, u.Name));
        mail.Subject = "Reset Your Password";
        mail.IsBodyHtml = true;

        // Generate the link: /Account/ResetPassword?token=XYZ&email=ABC
        var link = Url.Action("ResetPassword", "Account",
            new { token = token, email = u.Email },
            protocol: "https");

        // Photo Logic (kept from your code)
        var path = u switch
        {
            Manager => Path.Combine(en.WebRootPath, "images/users", "admin.jpg"),
            Staff s => Path.Combine(en.WebRootPath, "images/users", s.PhotoURL),
            _ => "",
        };

        if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
        {
            var att = new Attachment(path);
            mail.Attachments.Add(att);
            att.ContentId = "photo";
        }

        mail.Body = $@"
        <div style='font-family: Arial, sans-serif;'>
            <h2>Password Reset Request</h2>
            <p>Dear {u.Name},</p>
            <p>We received a request to reset your password. Click the link below to choose a new password:</p>
            <p>
                <a href='{link}' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>
                    Reset Password
                </a>
            </p>
            <p style='color: #666; font-size: 12px;'>This link expires in 30 minutes.</p>
            <hr>
            {(string.IsNullOrEmpty(path) ? "" : "<img src='cid:photo' style='width: 100px; height: 100px; border-radius: 50%; object-fit: cover;'>")}
        </div>
    ";

        hp.SendEmail(mail);
    }
}
