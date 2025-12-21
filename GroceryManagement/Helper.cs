using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using System.Net.Mail;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames.Image;

namespace GroceryManagement;

// TODO
public class Helper(IWebHostEnvironment en,
                    IHttpContextAccessor ct,
                    IConfiguration cf)
{
    // ------------------------------------------------------------------------
    // Photo Upload
    // ------------------------------------------------------------------------

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

    public string? SavePhoto(IFormFile f, string folder)
    {
        if (f == null || f.Length == 0)
        {
            return null;
        }
        try
        {
            var file = Guid.NewGuid().ToString("n") + ".jpg";
            var path = Path.Combine(en.WebRootPath, folder, file);

            // Create directory if missing
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            }
            // Load image
            using var img = Image.Load(f.OpenReadStream());

            // Config resize options 
            var options = new ResizeOptions
            {
                Size = new(800, 800),
                Mode = ResizeMode.Crop,
            };
            // Resize
            img.Mutate(x => x.Resize(options));

            //using var stream = new FileStream(path, FileMode.Create);

            img.SaveAsJpeg(path);

            return file;
        }
        catch (SixLabors.ImageSharp.UnknownImageFormatException)
        {
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void DeletePhoto(string file, string folder)
    {
        file = Path.GetFileName(file);
        var path = Path.Combine(en.WebRootPath, folder, file);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    // Helper: Finds the highest ID number in the DB and returns (Max + 1)
    public int GetNextExpenseSequence(DB db)
    {
        // 1. Fetch all IDs
        var existing = db.Expenses.Select(e => e.Id).ToList();

        var max = 0;
        foreach (var id in existing)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Strip "EX" prefix to get the number
            var digits = id.StartsWith("EX") ? id.Substring(2) : id;

            // Track the highest number found
            if (int.TryParse(digits, out var n)) max = Math.Max(max, n);
        }

        // Return the next number in the sequence
        return max + 1;
    }

    // ------------------------------------------------------------------------
    // Security Helper Functions
    // ------------------------------------------------------------------------


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
        if (role == "Staff")
        {
            rememberMe = false;
        }
        // Set expiry duration to 1 day for manager
        DateTime? expiry = rememberMe ? DateTime.UtcNow.AddDays(1) : null;

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
            // Force 1 day limit
            ExpiresUtc = expiry,
        };

        // (3) Sign in
        ct.HttpContext!.SignInAsync(principal, properties);
    }

    public void SignOut()
    {
        // Sign out
        ct.HttpContext!.SignOutAsync();
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
