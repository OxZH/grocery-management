using GroceryManagement.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using X.PagedList;
using X.PagedList.Extensions;

namespace GroceryManagement.Controllers;

public class HomeController(DB db, IWebHostEnvironment en, IHubContext<InventoryHub> hub) : Controller
{
    // GET: Home/Inde
    [Authorize]
    public IActionResult Index(string? name, string? sort, string? dir, int page = 1)
    {
        // Searching by ProductId
        ViewBag.Name = name = name?.Trim().ToUpper() ?? "";

        // Search Inventories where ProductId contains the search term
        var searched = db.Inventories.Where(i =>
        i.ProductId.Contains(name) ||
        i.Status.Contains(name));

        // Sorting
        ViewBag.Sort = sort;
        ViewBag.Dir = dir;

        var today = DateOnly.FromDateTime(DateTime.Today);
        var nearLimit = today.AddDays(3);
        //  sorting function 
        Func<Inventory, object> fn = sort switch
        {
            // sort by Id
            "Id" => i => i.Id.Substring(8, 1) + i.Id.Substring(3, 5),
            "ExpiryDate" => i => i.ExpiryDate,
            "ProductId" => i => i.ProductId,
            "StaffId" => i => i.StaffId,
            "Status" => i => i.Status,
            // default sort
            _ => i => i.Id.Substring(8, 1) + i.Id.Substring(3, 5),
        };
        var dataList = searched.AsEnumerable();

        var sorted = dir != "asc" ?
            dataList.OrderByDescending(i => i.Status == "AVAILABLE" && i.ExpiryDate < today)
            .ThenByDescending(i => i.Status == "AVAILABLE" && i.ExpiryDate <= nearLimit).ThenByDescending(fn) :
            dataList.OrderByDescending(i => i.Status == "AVAILABLE" && i.ExpiryDate < today)
                         .ThenByDescending(i => i.Status == "AVAILABLE" && i.ExpiryDate <= nearLimit).ThenBy(fn);
        // paging
        if (page < 1)
        {
            return RedirectToAction(null, new { name, sort, dir, page = 1 });
        }

        var s = sorted.ToPagedList(page, 10);

        if (page > s.PageCount && s.PageCount > 0)
        {
            return RedirectToAction(null, new { name, sort, dir, page = s.PageCount });
        }

        return View(s);
    }
    [Authorize]
    public IActionResult Insert()
    {
        ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
        ViewBag.SupplierList = new SelectList(db.Supplier.ToList(), "Id", "Id");
        return View();
    }

    // POST: Home/Insert
    [HttpPost]
    [Authorize]
    public IActionResult Insert(InventoryInsertVM vm)
    {
        vm.ProductId = vm.ProductId?.ToUpper();
        vm.SupplierId = vm.SupplierId?.ToUpper();
        //get current user staff id
        string? email = User.Identity?.Name;
        var staff = db.Staffs.FirstOrDefault(s => s.Email == email);

        if (staff == null)
        {
            ModelState.AddModelError("Unauthorize", "Error: Current user is not registered as a Staff member.");

            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
            ViewBag.SupplierList = new SelectList(db.Supplier.ToList(), "Id", "Id");
            return View(vm);
        }
        string StaffId = staff.Id;

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
        if (ModelState.IsValid("ProductId") &&!db.Products.Any(p => p.Id == vm.ProductId))
        {
            ModelState.AddModelError("ProductId", "Invalid Product.");
        }
        if (ModelState.IsValid("SupplierId") &&!db.Supplier.Any(p => p.Id == vm.SupplierId))
        {
            ModelState.AddModelError("SupplierId", "Invalid SupplierId.");
        }


        // check all is valid before proceeding
        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name");
            ViewBag.SupplierList = new SelectList(db.Supplier.ToList(), "Id", "Id");
            return View(vm);
        }

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
            ViewBag.SupplierList = new SelectList(db.Supplier.ToList(), "Id", "Id");
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

            // check if number exceeds 99999
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
                StaffId = StaffId,
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
            ViewBag.SupplierList = new SelectList(db.Supplier.ToList(), "Id", "Id");
            return View(vm);
        }
        var product = db.Products.Find(vm.ProductId);
        if (product != null)
        {
            // add the total inserted quantity to the existing WarehouseQty
            product.WareHouseQty += vm.Qty;
        }

        db.SaveChanges();
        TempData["Info"] = $"{vm.Qty} record(s) inserted. First ID: {firstId} to {lastId}.";
        return RedirectToAction("Index");
    }
    [Authorize]
    public IActionResult Update(string id)
    {

        var inv = db.Inventories.FirstOrDefault(i => i.Id == id);
        if (inv == null)
        {
            TempData["Info"] = $"Inventory ID {id} not found.";
            return RedirectToAction("Index");
        }
        ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name", inv.ProductId);
        //update 
        var vm = new InventoryUpdateVM
        {
            Id = inv.Id,
            ProductId = inv.ProductId,
            ExpiryDate = inv.ExpiryDate,
            StaffId = inv.StaffId,
            SupplierId = inv.SupplierId,
            CheckoutId = inv.CheckoutId,
            Status = inv.Status
        };
        return View(vm);
    }

    // POST: Home/Update
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Update(InventoryUpdateVM vm)
    {
        vm.ProductId = vm.ProductId?.ToUpper();
        vm.SupplierId = vm.SupplierId?.ToUpper();
        vm.CheckoutId = vm.CheckoutId?.ToUpper();

        // Validate ProductId
        if (ModelState.IsValid("ProductId") && !db.Products.Any(p => p.Id == vm.ProductId))
        {
            ModelState.AddModelError("ProductId", "Invalid Product.");
        }
        // Validate StaffId
        if (ModelState.IsValid("StaffId") && !db.Staffs.Any(s => s.Id == vm.StaffId))
        {
            ModelState.AddModelError("StaffId", "Invalid Staff Id.");
        }
        if (ModelState.IsValid("SupplierId") && !db.Supplier.Any(p => p.Id == vm.SupplierId))
        {
            ModelState.AddModelError("SupplierId", "Invalid SupplierId.");
        }
        // Validate ExpiryDate
        if (ModelState.IsValid("ExpiryDate") && vm.ExpiryDate < DateTime.Today.ToDateOnly())
        {
            ModelState.AddModelError("ExpiryDate", "Expiry Date cannot be a past date.");
        }
        //Validate CheckoutId
        if (ModelState.IsValid("CheckoutId") && !string.IsNullOrEmpty(vm.CheckoutId) && !db.Checkout.Any(c => c.Id == vm.CheckoutId))
        {
            ModelState.AddModelError("CheckoutId", "Invalid Checkout ID. Record does not exist.");
        }
        var validStatus = new[] { "AVAILABLE", "SOLD", "DISPOSED" };
        // Validate Status 
        if (ModelState.IsValid("Status"))
        {
            if (!validStatus.Contains(vm.Status))
            {
                ModelState.AddModelError("Status", "Invalid Status. Must be INSTOCK, SOLD, or DISPOSED.");
            }
            else if (vm.Status == "SOLD" && string.IsNullOrEmpty(vm.CheckoutId))
            {
                ModelState.AddModelError("CheckoutId", "Checkout ID is required when status is SOLD.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProductList = new SelectList(db.Products.ToList(), "Id", "Name", vm.ProductId);
            return View(vm);
        }

        // retrieve the record from the database
        var inv = db.Inventories.FirstOrDefault(i => i.Id == vm.Id);
        if (inv == null)
        {
            TempData["Info"] = $"Error: Inventory ID {vm.Id} not found.";
            return RedirectToAction("Index");
        }
        if (inv.ProductId != vm.ProductId)
        {
            var oldProduct = db.Products.Find(inv.ProductId);
            if (oldProduct != null && oldProduct.WareHouseQty > 0)
            {
                oldProduct.WareHouseQty -= 1;
            }
            var newProduct = db.Products.Find(vm.ProductId);
            if (newProduct != null)
            {
                newProduct.WareHouseQty += 1;
            }
        }
        if (inv.Status != vm.Status)
        {
            var currentProduct = db.Products.Find(vm.ProductId);

            if (currentProduct != null)
            {
                // change available to sold/disposed
                if (inv.Status == "AVAILABLE" && vm.Status != "AVAILABLE")
                {
                    if (currentProduct.WareHouseQty > 0)
                    {
                        currentProduct.WareHouseQty -= 1;
                    }
                    else
                    {
                        TempData["Info"] = $"Error: Product {vm.ProductId} warehosueqty id less than 1.";
                        return RedirectToAction("Index");
                    }
                }
                // change sold/disposed to available
                else if (inv.Status != "AVAILABLE" && vm.Status == "AVAILABLE")
                {
                    currentProduct.WareHouseQty += 1;
                }
            }
        }

        // apply changes and save
        inv.ProductId = vm.ProductId;
        inv.ExpiryDate = vm.ExpiryDate;
        inv.StaffId = vm.StaffId;
        inv.SupplierId = vm.SupplierId;
        inv.Status = vm.Status;

        if (vm.Status == "SOLD")
        {
            inv.CheckoutId = vm.CheckoutId?.Trim();
        }
        else
        {
            inv.CheckoutId = null; // clear CheckoutId if not SOLD
        }

        db.SaveChanges();
        await hub.Clients.All.SendAsync("ReceiveUpdate", inv.Id, inv.Status, inv.CheckoutId ?? "");

        TempData["Info"] = $"Record {vm.Id} updated successfully.";

        return RedirectToAction("Index");
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteConfirmed(string id)
    {
        var inv = db.Inventories.Find(id);

        // validate id
        if (inv == null)
        {
            TempData["Info"] = $"Error: Inventory ID {id} not found.";
            return RedirectToAction("Index");
        }
        // find the product id from table
        var product = db.Products.Find(inv.ProductId);
        if (product != null)
        {
            if (product.WareHouseQty < 1)
            {
                TempData["Info"] = $"Error: Cannot delete. Product Warehouse Quantity is already {product.WareHouseQty}.";
                return RedirectToAction("Index");
            }

            // qty - 1
            product.WareHouseQty -= 1;
        }
        db.Inventories.Remove(inv);
        db.SaveChanges();

        TempData["Info"] = $"Inventory record {id} deleted successfully.";
        return RedirectToAction("Index");
    }
    [Authorize]
    public IActionResult Detail(string id, int page = 1)
    {
        // Find the inventory record by ID
        var inv = db.Inventories.FirstOrDefault(i => i.Id == id);

        if (inv == null)
        {
            TempData["Info"] = $"Inventory ID {id} not found.";
            return RedirectToAction("Index");
        }
        // list all batch with same ProductId, ExpiryDate, StaffId
        var batchList = db.Inventories
        .Where(i => i.ProductId == inv.ProductId
                 && i.ExpiryDate == inv.ExpiryDate
                 && i.StaffId == inv.StaffId
                 && i.Id != inv.Id)
        .OrderBy(i => i.Id)
        .ToPagedList(page, 10);

        ViewBag.BatchList = batchList;
        return View(inv);
    }

    [HttpPost]
    [Authorize]
    public IActionResult DeleteBatch(string id)
    {
        // find the template id
        var template = db.Inventories.Find(id);


        if (template == null)
        {
            TempData["Info"] = "Error: Record not found.";
            return RedirectToAction("Index");
        }

        // find all items that match this batch
        var batchToDelete = db.Inventories
            .Where(i => i.ProductId == template.ProductId
                     && i.ExpiryDate == template.ExpiryDate
                     && i.Status == template.Status
                     && i.StaffId == template.StaffId)

            .ToList();
        int count = batchToDelete.Count;

        if (count > 0)
        {
            var product = db.Products.Find(template.ProductId);
            if (product != null)
            {
                //validate warehouse qty
                if (product.WareHouseQty < count)
                {
                    TempData["Info"] = $"Error: Cannot delete batch. Warehouse Qty ({product.WareHouseQty}) is less than the batch size ({count}).";
                    return RedirectToAction("Index");
                }
                //warehouse qty - batch count
                product.WareHouseQty -= count; 
            }
            db.Inventories.RemoveRange(batchToDelete);
            db.SaveChanges();
            TempData["Info"] = $"Success: Deleted entire batch ({count} records).";
        }

        return RedirectToAction("Index");
    }
}