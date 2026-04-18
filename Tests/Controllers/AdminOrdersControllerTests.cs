using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.ViewModels.Admin;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminOrdersControllerTests
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
        public async Task Index_ReturnsViewResult_WithOrders()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" };
            context.Facilities.Add(facility);
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 });
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.Index(null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminOrderListViewModel>(viewResult.Model);
            Assert.Single(model.Orders);
            Assert.Equal(OrderStatus.Created, model.Orders.First().OrderStatus);
        }

        [Fact]
        public async Task Index_FiltersByStatus()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" };
            context.Facilities.Add(facility);
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 });
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Scanned, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 200 });
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.Index(null, null, OrderStatus.Scanned, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminOrderListViewModel>(viewResult.Model);
            Assert.Single(model.Orders);
            Assert.Equal(OrderStatus.Scanned, model.Orders.First().OrderStatus);
        }

        [Fact]
        public async Task UpdateStatus_UpdatesStatus_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, TotalAmount = 100 };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.UpdateStatus(order.Id, OrderStatus.Scanned);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var updatedOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Scanned, updatedOrder.OrderStatus);
        }

        [Fact]
        public async Task Details_ReturnsView_WithOrder()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "123", RepresentativeName = "Rep" };
            context.Facilities.Add(facility);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.Details(order.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Order>(viewResult.Model);
            Assert.Equal(order.Id, model.Id);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.Details(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.UpdateStatus(999, OrderStatus.Scanned);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateStatus_RedirectsToIndex_WhenInvalidStatusTransition()
        {
            // Arrange
            var context = GetDatabaseContext();
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, TotalAmount = 100 };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.UpdateStatus(order.Id, OrderStatus.Delivered);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var updatedOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Created, updatedOrder.OrderStatus);
        }
    }
}
