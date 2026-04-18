using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StockFlowPro.Data;
using StockFlowPro.Models;
using System;
using System.Threading.Tasks;
using Xunit;

namespace StockFlowPro.Tests.Data
{
    public class SeedDataTests
    {
        [Fact]
        public async Task Initialize_SeedsData_IntoDatabase()
        {
            // Arrange
            var services = new ServiceCollection();
            
            services.AddDbContext<FoodieDbContext>(options =>
                options.UseInMemoryDatabase("SeedDataTest"));

            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<FoodieDbContext>()
                .AddDefaultTokenProviders();

            services.AddLogging(builder => builder.AddConsole());

            var serviceProvider = services.BuildServiceProvider();

            // Create roles first because SeedData adds users to roles
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var roles = new[] { "OfficeWorker", "Scanner", "Packer", "Admin" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }
            }

            // Act
            await SeedData.Initialize(serviceProvider);

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<FoodieDbContext>();
                Assert.Equal(100, await context.Products.CountAsync());
                Assert.Equal(30, await context.Facilities.CountAsync());
                Assert.Equal(20, await context.Employees.CountAsync());
                Assert.Equal(30, await context.Orders.CountAsync());
            }
        }
    }
}
