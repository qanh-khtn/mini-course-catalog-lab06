using Microsoft.AspNetCore.Identity;
using MiniCourseCatalog.Mvc.Models;

namespace MiniCourseCatalog.Mvc.Data;

public static class DbInitializer
{
    public static async Task SeedIdentityAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Staff", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var adminEmail = "admin@coursecenter.test";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail, FullName = "System Admin", Department = "IT" };
            await userManager.CreateAsync(adminUser, "Admin@123");
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var staffEmail = "staff@coursecenter.test";
        if (await userManager.FindByEmailAsync(staffEmail) == null)
        {
            var staffUser = new ApplicationUser { UserName = staffEmail, Email = staffEmail, FullName = "Course Staff", Department = "Operations" };
            await userManager.CreateAsync(staffUser, "Staff@123");
            await userManager.AddToRoleAsync(staffUser, "Staff");
        }

        var userEmail = "user@coursecenter.test";
        if (await userManager.FindByEmailAsync(userEmail) == null)
        {
            var normalUser = new ApplicationUser { UserName = userEmail, Email = userEmail, FullName = "Normal User", Department = "None" };
            await userManager.CreateAsync(normalUser, "User@123");
            await userManager.AddToRoleAsync(normalUser, "User");
        }
    }
}
