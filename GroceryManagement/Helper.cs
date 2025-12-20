//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
//using System.Security.Claims;
using System.Text.RegularExpressions;
//using static System.Net.Mime.MediaTypeNames;

namespace GroceryManagement;

public class Helper(IWebHostEnvironment en,
                    IHttpContextAccessor ct,
                    IConfiguration cf)
{
    //------------------------------------------------------------------------
    //Photo Upload
    //------------------------------------------------------------------------

    public string ValidatePhoto(IFormFile f)
    {
        var reType = new Regex(@"^image\/(jpeg|png)$", RegexOptions.IgnoreCase);
        var reName = new Regex(@"^.+\.(jpeg|jpg|png)$", RegexOptions.IgnoreCase);

        if (!reType.IsMatch(f.ContentType) || !reName.IsMatch(f.FileName))
        {
            return "Only JPG and PNG photo is allowed.";
        }
        else if (f.Length > 1 * 5120 * 5120)
        {
            return "Photo size cannot more than 5MB.";
        }

        return "";
    }

    public string SavePhoto(IFormFile f, string folder)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        var options = new ResizeOptions
        {
            Size = new(200, 200),
            Mode = ResizeMode.Crop,
        };

        using var stream = f.OpenReadStream();
        using var img = Image.Load(stream);
        img.Mutate(x => x.Resize(options));
        img.Save(path);

        return file;
    }
    public string ProSavePhoto(IFormFile f, string folder, int rotateDegrees = 0, bool flip = false)
    {
        var file = Guid.NewGuid().ToString("n") + ".jpg";
        var path = Path.Combine(en.WebRootPath, folder, file);

        if (!Directory.Exists(Path.GetDirectoryName(path)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        using var img = Image.Load(f.OpenReadStream());
        // 1. APPLY ROTATION (Advanced Feature)
        if (rotateDegrees != 0)
        {
            img.Mutate(x => x.Rotate(rotateDegrees));
        }

        // 2. APPLY FLIP (Advanced Feature)
        if (flip)
        {
            img.Mutate(x => x.Flip(FlipMode.Horizontal));
        }

        // 3. Then Resize
        var options = new ResizeOptions
        {
            Size = new(800, 800),
            Mode = ResizeMode.Crop,
        };
        img.Mutate(x => x.Resize(options));

        img.SaveAsJpeg(path);

        return file;
    }
    public string ProCurrentPhoto(string fileName, string folder, int degrees, bool flip)
    {
        // 1. Find the existing file on the server
        var oldPath = Path.Combine(en.WebRootPath, folder, fileName);

        // Safety check: if file doesn't exist, do nothing
        if (!File.Exists(oldPath)) return fileName;

        // 2. Load the image
        using var img = Image.Load(oldPath);

        // 3. Apply Rotate/Flip
        if (degrees != 0) img.Mutate(x => x.Rotate(degrees));
        if (flip) img.Mutate(x => x.Flip(FlipMode.Horizontal));

        // 4. Save as a NEW file (Important! This forces the browser to see the change)
        var newFileName = Guid.NewGuid().ToString("n") + ".jpg";
        var newPath = Path.Combine(en.WebRootPath, folder, newFileName);

        // Maintain the standard size (optional, ensures consistency)
        img.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new(800, 800),
            Mode = ResizeMode.Crop
        }));

        img.SaveAsJpeg(newPath);

        // 5. Delete the old file to save space
        // (Note: In a real app, you might keep backups, but for this assignment, deleting is fine)
        try { File.Delete(oldPath); } catch { }

        return newFileName; // Return the new name so the Database can update
    }
    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        File.Delete(path);
    }



    //// ------------------------------------------------------------------------
    //// Security Helper Functions
    //// ------------------------------------------------------------------------
    private readonly PasswordHasher<object> ph = new();

    public string HashPassword(string password)
    {
        return ph.HashPassword(0, password);
    }

    public bool VerifyPassword(string hash, string password)
    {
        return ph.VerifyHashedPassword(0, hash, password)
                == PasswordVerificationResult.Success;
    }

    public void SignIn(string email, string role, bool rememberMe, string phone)
    {
        // (1) Claim, identity and principal
        List<Claim> claims =
        [
            new(ClaimTypes.Name, email),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.MobilePhone, phone ?? "")
        ];


        ClaimsIdentity identity = new(claims, "Cookies");

        ClaimsPrincipal principal = new(identity);

        // (2) Remember me (authentication properties)
        AuthenticationProperties properties = new()
        {
            IsPersistent = rememberMe,
        };

        // (3) Sign in
        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        // Sign out
        ct.HttpContext!.SignOutAsync();
    }

    public string RandomPassword()
    {
        string s = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string password = "";

        Random r = new();

        for (int i = 1; i <= 10; i++)
        {
            password += s[r.Next(s.Length)]; //s[r.Next(36)] -> s[10] so the random value is A
        }

        return password;
    }

    // ------------------------------------------------------------------------
    // Email Helper Functions
    // ------------------------------------------------------------------------

    //here can add async vers if no errors found, public async void SendEmail(mail)
    public void SendEmail(MailMessage mail)
    {
        string user = cf["Smtp:User"] ?? "";
        string pass = cf["Smtp:Pass"] ?? "";
        string name = cf["Smtp:Name"] ?? "";
        string host = cf["Smtp:Host"] ?? "";
        int port = cf.GetValue<int>("Smtp:Port");

        mail.From = new MailAddress(user, name);

        using var smtp = new SmtpClient
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass),
        };
        //Console.WriteLine(user, pass, name, host);
        // can change to async ver after no errors found await smtp.SendMailAsync(mail);
        smtp.Send(mail);
    }



    // ------------------------------------------------------------------------
    // DateTime Helper Functions
    // ------------------------------------------------------------------------

    // Return January (1) to December (12)
    public SelectList GetMonthList()
    {
        var list = new List<object>();

        for (int n = 1; n <= 12; n++)
        {
            list.Add(new
            {
                Id = n,
                Name = new DateTime(1, n, 1).ToString("MMMM"),
            });
        }

        return new SelectList(list, "Id", "Name");
    }

    // Return min to max years
    public SelectList GetYearList(int min, int max, bool reverse = false)
    {
        var list = new List<int>();

        for (int n = min; n <= max; n++)
        {
            list.Add(n);
        }

        if (reverse) list.Reverse();

        return new SelectList(list);
    }
}
