using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockFlowPro.Models;

namespace StockFlowPro.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new FoodieDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<FoodieDbContext>>()))
            {
                context.Database.EnsureCreated();
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            // Removed "Driver" from roles
            string[] roleNames = { "Admin", "OfficeWorker", "Scanner", "Packer" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed Admin User
            var adminEmail = "admin@stockflow.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var user = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(user, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // Seed Office Worker
            var officeEmail = "office@stockflow.com";
            if (await userManager.FindByEmailAsync(officeEmail) == null)
            {
                var user = new IdentityUser { UserName = officeEmail, Email = officeEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, "Office123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(user, "OfficeWorker");
            }

            // Seed Scanner
            var scannerEmail = "scanner@stockflow.com";
            if (await userManager.FindByEmailAsync(scannerEmail) == null)
            {
                var user = new IdentityUser { UserName = scannerEmail, Email = scannerEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, "Scanner123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(user, "Scanner");
            }

            // Seed Packer
            var packerEmail = "packer@stockflow.com";
            if (await userManager.FindByEmailAsync(packerEmail) == null)
            {
                var user = new IdentityUser { UserName = packerEmail, Email = packerEmail, EmailConfirmed = true };
                var result = await userManager.CreateAsync(user, "Packer123!");
                if (result.Succeeded) await userManager.AddToRoleAsync(user, "Packer");
            }

            // Removed Driver Seeding
        }
    }
}
