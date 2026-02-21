using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Services;
using StockFlowPro.ViewModels.Order;
using Xunit;

namespace StockFlowPro.Tests
{
    public class DatabaseWriteTests
    {
        [Fact]
        public async Task CreateOrderAsync_PersistsOrderAndItems()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StockFlowDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new StockFlowDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var facility = new Facility
            {
                Name = "Main",
                Address = "123 St",
                Phone = "555-0000",
                Area = "North",
                RepresentativeName = "Sam Lead",
                IsActive = true
            };
            var product = new Product
            {
                Name = "Widget",
                Brand = "Acme",
                Price = 10m,
                QuantityInStock = 100,
                MinimumStockLevel = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.Facilities.Add(facility);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context, NullLogger<OrderService>.Instance);
            var order = await service.CreateOrderAsync(facility.Id, new()
            {
                new OrderItemDto { ProductId = product.Id, Quantity = 2 }
            });

            var persisted = await context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            Assert.NotNull(persisted);
            Assert.Single(persisted.OrderItems);
            Assert.Equal(20m, persisted.TotalAmount);
        }

        [Fact]
        public async Task OrderControllerCreate_FromOfficePage_PersistsOrder()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var options = new DbContextOptionsBuilder<StockFlowDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new StockFlowDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var facility = new Facility
            {
                Name = "Main",
                Address = "123 St",
                Phone = "555-0000",
                Area = "North",
                RepresentativeName = "Sam Lead",
                IsActive = true
            };
            var product = new Product
            {
                Name = "Widget",
                Brand = "Acme",
                Price = 10m,
                QuantityInStock = 100,
                MinimumStockLevel = 1,
                CreatedAt = DateTime.UtcNow
            };
            context.Facilities.Add(facility);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context, NullLogger<OrderService>.Instance);
            var controller = new OrderController(service, context, NullLogger<OrderController>.Instance);
            var model = new CreateOrderViewModel
            {
                FacilityId = facility.Id,
                OrderItems =
                {
                    new OrderItemViewModel { ProductId = product.Id, Quantity = 3 }
                }
            };

            var result = await controller.Create(model);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Single(context.Orders.ToList());
            Assert.Single(context.OrderItems.ToList());
        }

        [Fact]
        public async Task AdminEmployeeCreate_PersistsEmployeeAndUser()
        {
            using var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<StockFlowDbContext>(options => options.UseSqlite(connection));
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<StockFlowDbContext>()
                .AddDefaultTokenProviders();

            var provider = services.BuildServiceProvider();
            await using var scope = provider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<StockFlowDbContext>();
            await context.Database.EnsureCreatedAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await roleManager.CreateAsync(new IdentityRole("OfficeWorker"));
            await roleManager.CreateAsync(new IdentityRole("Scanner"));
            await roleManager.CreateAsync(new IdentityRole("Packer"));
            await roleManager.CreateAsync(new IdentityRole("Driver"));
            var controller = new AdminEmployeesController(context, userManager, NullLogger<AdminEmployeesController>.Instance);

            var employee = new Employee
            {
                FullName = "Test User",
                Position = "OfficeWorker",
                Phone = "555-1234"
            };

            var result = await controller.Create(employee, "testuser@stockflow.com", "Pass123!");

            Assert.NotNull(result);
            Assert.Single(context.Employees.ToList());
            Assert.Single(userManager.Users.ToList());
        }
    }
}
