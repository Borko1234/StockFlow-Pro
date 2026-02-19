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

                // Look for any users.
                if (context.Users.Any())
                {
                    return;   // DB has been seeded
                }
            }

            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string roleName = "Admin";
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            var user = new IdentityUser
            {
                UserName = "admin@foodie.com",
                Email = "admin@foodie.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "Admin123!");

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, roleName);
            }
        }
    }
}
