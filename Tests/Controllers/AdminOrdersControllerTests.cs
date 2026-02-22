using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.ViewModels.Admin;
using StockFlowPro.Models.Enums;
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
            var facility = new Facility { Name = "Test Facility", Address = "123 Test St", Phone = "555-1234", RepresentativeName = "John Doe" };
            context.Facilities.Add(facility);
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility });
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Now.AddDays(-1), Facility = facility });
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.Index(null, null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<AdminOrderListViewModel>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Orders.Count);
        }

        [Fact]
        public async Task Index_FiltersByStatus()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Test Facility", Address = "123 Test St", Phone = "555-1234", RepresentativeName = "John Doe" };
            context.Facilities.Add(facility);
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility });
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Now.AddDays(-1), Facility = facility });
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.Index(null, null, OrderStatus.Created, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<AdminOrderListViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Orders);
            Assert.Equal(OrderStatus.Created, model.Orders.First().OrderStatus);
        }

        [Fact]
        public async Task UpdateStatus_UpdatesStatus_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" } };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.UpdateStatus(order.Id, OrderStatus.Prepared);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var updatedOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Prepared, updatedOrder.OrderStatus);
        }

        [Fact]
        public async Task UpdateStatus_ClearsPreparedBy_WhenStatusIsCreated()
        {
            // Arrange
            var context = GetDatabaseContext();
            var employee = new Employee { FullName = "Packer", Position = "Packer", Phone = "123" };
            context.Employees.Add(employee);
            var order = new Order 
            { 
                OrderStatus = OrderStatus.Prepared, 
                CreatedAt = DateTime.Now, 
                Facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" },
                OrderProcessing = new OrderProcessing { PreparedByEmployeeId = employee.Id }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new AdminOrdersController(context);

            // Act
            var result = await controller.UpdateStatus(order.Id, OrderStatus.Created);

            // Assert
            var updatedOrder = await context.Orders.Include(o => o.OrderProcessing).FirstOrDefaultAsync(o => o.Id == order.Id);
            Assert.Equal(OrderStatus.Created, updatedOrder.OrderStatus);
            Assert.Null(updatedOrder.OrderProcessing.PreparedByEmployeeId);
        }
    }
}
