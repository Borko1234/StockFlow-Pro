using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.ViewModels.Admin;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminControllerTests
    {
        private FoodieDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<FoodieDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new FoodieDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task Index_ReturnsDashboardViewModel_WithCorrectStats()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "1" };
            context.Facilities.Add(facility);
            
            var product = new Product { Name = "P1", Brand = "B1", Price = 100, CostPrice = 50 };
            context.Products.Add(product);

            var order1 = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 };
            var order2 = new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 200 };
            context.Orders.AddRange(order1, order2);

            context.OrderItems.Add(new OrderItem { Order = order2, Product = product, Quantity = 2, TotalPrice = 200 });

            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminDashboardViewModel>(viewResult.Model);

            Assert.Equal(2, model.TotalOrders);
            Assert.Equal(1, model.PendingOrders);
            Assert.Equal(200, model.TotalRevenue); // Only delivered orders
            Assert.Single(model.TopProducts);
            Assert.Equal("P1", model.TopProducts.First().ProductName);
        }
    }
}
