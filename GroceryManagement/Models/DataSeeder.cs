using Microsoft.AspNetCore.Identity;
using System;
using GroceryManagement.Models;

namespace GroceryManagement.Models;

public class DataSeeder
{
    public static void SeedManager(DB db, Helper hp)
    {
        // Only run if NO managers exist
        if (db.Managers.Any())
        {
            return; // Database is fine, do nothing.
        }

        // Create temp Admin
        var rescueManager = new Manager
        {
            Id = "M001", // Hardcode the first ID
            Name = "System Administrator",
            Email = "admin@gmail.com",

            // 3. Set a Known Default Password (e.g., "admin123")
            // The user MUST change this immediately after logging in.
            Password = hp.HashPassword("admin123"),

            PhoneNum = "000-0000000",
        };

        db.Managers.Add(rescueManager);
        db.SaveChanges();
    }
}
