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
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class PackerControllerTests
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
        public async Task Index_ReturnsViewResult_WithScannedOrders()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "1" };
            context.Facilities.Add(facility);
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Scanned, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 });
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 200 });
            await context.SaveChangesAsync();

            var controller = new PackerController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Order>>(viewResult.ViewData.Model);
            Assert.Equal(1, model.Count);
            Assert.Equal(OrderStatus.Scanned, model.First().OrderStatus);
        }

        [Fact]
        public async Task MarkPacked_RedirectsToIndex()
        {
            // Arrange
            var context = GetDatabaseContext();
            var order = new Order { OrderStatus = OrderStatus.Scanned, CreatedAt = DateTime.Now, TotalAmount = 100 };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new PackerController(context);

            // Act
            var result = await controller.MarkPacked(order.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
        }
    }
}
