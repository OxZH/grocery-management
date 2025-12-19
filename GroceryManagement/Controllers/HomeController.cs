using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList;
using X.PagedList.Extensions;

namespace GroceryManagement.Controllers;

public class HomeController(DB db, IWebHostEnvironment en) : Controller
{
    // GET: Home/Index
    public IActionResult Index(string? name, string? sort, string? dir, int page = 1)
    {
        // Searching by ProductId
        ViewBag.Name = name = name?.Trim().ToUpper() ?? "";

        // Search Inventories where ProductId contains the search term
        var searched = db.Inventories.Where(i => 
        i.ProductId.Contains(name) ||
        i.Status.Contains(name));

        // (2) Sorting
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        // Define the sorting function based on the column name
        Func<Inventory, object> fn = sort switch
        {
            // Sorting by Id uses a composite key to ensure correct sequence (INV00001A, INV00002A, INV00001B)
            "Id" => i => i.Id.Substring(8, 1) + i.Id.Substring(3, 5),
            "ExpiryDate" => i => i.ExpiryDate,
            "ProductId" => i => i.ProductId,
            "StaffId" => i => i.StaffId,
            "Status" => i => i.Status,
            // Default sort: latest ID first (composite descending)
            _ => i => i.Id.Substring(8, 1) + i.Id.Substring(3, 5),
            //_ => searched.OrderByDescending(i => i.Id.Substring(8, 1)).ThenByDescending(i => i.Id.Substring(3, 5)),
        };

        var sorted = dir != "asc" ?
                     searched.OrderByDescending(fn) :
                     searched.OrderBy(fn);

        // (3) Paging
        if (page < 1)
        {
            return RedirectToAction(null, new { name, sort, dir, page = 1 });
        }

        // Apply Paging (Requires X.PagedList NuGet package)
        var m = sorted.ToPagedList(page, 10);

        if (page > m.PageCount && m.PageCount > 0)
        {
            return RedirectToAction(null, new { name, sort, dir, page = m.PageCount });
        }

        return View(m);
    }
    public IActionResult Insert()
    {
        ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
        return View();
    }

    // POST: Home/Insert
    [HttpPost]
    public IActionResult Insert(InventoryInsertVM vm)
    {
        //---validate user input---
        // check if the ExpiryDate is in the past
        if (ModelState.IsValid("ExpiryDate"))
        {
            var today = DateTime.Today.ToDateOnly();
            if (vm.ExpiryDate < today)
            {
                ModelState.AddModelError("ExpiryDate", "Expiry Date cannot be a past date.");
            }
        }

        // check if the ProductId is valid
        if (ModelState.IsValid("ProductId") &&
            !db.Products.Any(p => p.Id == vm.ProductId))
        {
            ModelState.AddModelError("ProductId", "Invalid Product.");
        }

        // check all is valid before proceeding
        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
            return View(vm);
        }

        // ---id generation---
        // find the lastest id
        var maxId = db.Inventories
            .Where(i => i.Id.Length == 9) // id must be 9 characters
            .OrderByDescending(i => i.Id.Substring(8, 1)) // sort by Z to A
            .ThenByDescending(i => i.Id.Substring(3, 5))  // sort by 99999 to 00001
            .Select(i => (string?)i.Id)
            .FirstOrDefault();

        int currentNumber = 0;
        char currentLetter = 'A';

        if (maxId != null)
        {
            // extract number and letter from the max ID
            string numPart = maxId.Substring(3, 5);
            currentLetter = maxId[8];
            //validate maxId format 
            if (!int.TryParse(numPart, out currentNumber))
            {
                ModelState.AddModelError("", "Internal error: Could not parse max Inventory ID number.");
            }
        }

        // check if a critical internal error occurred during max ID parsing
        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
            return View(vm);
        }

        // multiple record insert

        List<Inventory> newRecords = new List<Inventory>();

        int count = vm.Qty;
        string firstId = "", lastId = "";

        for (int i = 0; i < count; i++)
        {
            // determine the next id
            int nextNumber = currentNumber + 1;
            char nextLetter = currentLetter;

            // check if number exceeds 99999)
            if (nextNumber > 99999)
            {
                nextNumber = 1;

                // increment the letter
                if (currentLetter == 'Z')
                {
                    ModelState.AddModelError("", "Inventory ID capacity reached (INV99999Z). Cannot insert all requested records.");
                    break;
                }
                nextLetter = (char)(currentLetter + 1);
            }

            string nextNumPart = nextNumber.ToString("D5");
            string newId = $"INV{nextNumPart}{nextLetter}";

            if (i == 0)
            {
                firstId = newId;
            }
            lastId = newId;

            // create the Inventory record and add to the list
            db.Inventories.Add(new()
            {
                Id = newId,
                ProductId = vm.ProductId,
                ExpiryDate = vm.ExpiryDate,
                Status = "AVAILABLE",
                StaffId = "S001",
                SupplierId = vm.SupplierId
            });

            // update for the next loop
            currentNumber = nextNumber;
            currentLetter = nextLetter;
        }

        // final check for ID capacity error added during the loop
        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
            return View(vm);
        }

        // to add all generated records to the context
        db.Inventories.AddRange(newRecords);
        db.SaveChanges();

        TempData["Info"] = $"{newRecords.Count} record(s) inserted. First ID: {firstId} to {lastId}.";
        return RedirectToAction("Index");

    }

    public IActionResult Update(string id)
    {
        var inv = db.Inventories.FirstOrDefault(i => i.Id == id);
        if (inv == null)
        {
            TempData["Info"] = $"Inventory ID {id} not found.";
            return RedirectToAction("Index");
        }

        // Populate ViewBag with Product List for the dropdown
        ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name", inv.ProductId);

        // Populate ViewBag with Staff List for the new dropdown
        //ViewBag.StaffList = new SelectList(db.Staffs.ToList(), "Id", "Name", inv.StaffId);

        // Map the entity to the Update View Model (Assuming CheckoutId and Status exist on Inventory entity)
        var vm = new InventoryUpdateVM
        {
            Id = inv.Id,
            ProductId = inv.ProductId,
            ExpiryDate = inv.ExpiryDate,
            StaffId = inv.StaffId,
            CheckoutId = inv.CheckoutId,
            Status = inv.Status
        };

        return View(vm);
    }

    // POST: Home/Update
    [HttpPost]
    public IActionResult Update(InventoryUpdateVM vm)
    {
        // 1. Validation 

        // Validate ProductId
        if (ModelState.IsValid("ProductId") &&
            !db.Products.Any(p => p.Id == vm.ProductId))
        {
            ModelState.AddModelError("ProductId", "Invalid PRODUCT.");
        }

        //// Validate StaffId
        //if (ModelState.IsValid("StaffId") &&
        //    !db.Staffs.Any(s => s.Id == vm.StaffId))
        //{
        //    ModelState.AddModelError("StaffId", "Invalid STAFF ID.");
        //}

        // Validate ExpiryDate
        if (ModelState.IsValid("ExpiryDate"))
        {
            var today = DateTime.Today.ToDateOnly();
            if (vm.ExpiryDate < today)
            {
                ModelState.AddModelError("ExpiryDate", "Expiry Date cannot be a past date.");
            }
        }

        // Validate CheckoutId (must be null/empty or exist in the Checkout table)
        //if (ModelState.IsValid("CheckoutId") && !string.IsNullOrEmpty(vm.CheckoutId) &&
        //    !db.Checkouts.Any(c => c.Id == vm.CheckoutId))
        //{
        //    ModelState.AddModelError("CheckoutId", "Invalid Checkout ID. Record does not exist.");
        //}

        // Validate Status (must match assumed valid options: InStock, Sold, Disposed)
        if (ModelState.IsValid("Status"))
        {
            var validStatuses = new[] { "AVAILABLE", "SOLD", "DISPOSED" };
            if (!validStatuses.Contains(vm.Status))
            {
                ModelState.AddModelError("Status", "Invalid Status. Must be INSTOCK, SOLD, or DISPOSED.");
            }
        }

        if (!ModelState.IsValid)
        {
            // Re-populate ViewBags on failure
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name", vm.ProductId);
            ViewBag.StaffList = new SelectList(db.Staffs.ToList(), "Id", "Name", vm.StaffId);
            return View(vm);
        }

        // 2. Retrieve the record from the database
        var inv = db.Inventories.FirstOrDefault(i => i.Id == vm.Id);
        if (inv == null)
        {
            TempData["Info"] = $"Error: Inventory ID {vm.Id} not found during update.";
            return RedirectToAction("Index");
        }

        // 3. Apply changes and save
        inv.ProductId = vm.ProductId;
        inv.ExpiryDate = vm.ExpiryDate;
        inv.StaffId = vm.StaffId;
        inv.CheckoutId = vm.CheckoutId?.Trim(); // Save null if empty/whitespace
        inv.Status = vm.Status;

        db.SaveChanges();
        TempData["Info"] = $"Record {vm.Id} updated successfully.";

        return RedirectToAction("Index");
    }

    public IActionResult Delete(string id)
    {
        // Find the inventory record by ID
        var inv = db.Inventories
            .FirstOrDefault(i => i.Id == id);

        if (inv == null)
        {
            TempData["Info"] = $"Inventory ID {id} not found.";
            return RedirectToAction("Index");
        }

        // Pass the Inventory object to the confirmation view
        return View(inv);
    }

    // POST: Home/Delete/INV00001A(Execution)
    [HttpPost]
    public IActionResult Delete(Inventory inv)
    {
        // Find the entity to be deleted in the database.
        // We use the ID from the posted Inventory object.
        var inventoryToDelete = db.Inventories.FirstOrDefault(i => i.Id == inv.Id);

        if (inventoryToDelete == null)
        {
            TempData["Info"] = $"Error: Inventory ID {inv.Id} not found. Cannot delete.";
            return RedirectToAction("Index");
        }

        // Remove the entity and save changes
        db.Inventories.Remove(inventoryToDelete);
        db.SaveChanges();

        TempData["Info"] = $"Inventory record {inv.Id} deleted successfully.";
        return RedirectToAction("Index");
    }

    public IActionResult Detail(string id)
    {
        // Find the inventory record by ID
        var inv = db.Inventories
            .FirstOrDefault(i => i.Id == id);

        if (inv == null)
        {
            TempData["Info"] = $"Inventory ID {id} not found.";
            return RedirectToAction("Index");
        }

        // Pass the Inventory object to the view
        return View(inv);
    }

    public IActionResult TestDBUsers()
    {
        var users = db.Users;
        return View(users);
    }


}
