using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.ViewModels;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class ScannerControllerTests
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
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new ScannerController(new Mock<IOrderService>().Object, context);
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_ShowsSuccessMessage_WhenProvided()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new ScannerController(new Mock<IOrderService>().Object, context);
            var tempData = new Mock<ITempDataDictionary>();
            tempData.Setup(t => t["SuccessMessage"]).Returns("Scan successful!");
            controller.TempData = tempData.Object;

            // Act
            var result = controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            Assert.Equal("Scan successful!", model.SuccessMessage);
        }

        [Fact]
        public async Task Scan_ValidOrder_ReturnsViewWithModel()
        {
            // Arrange
            var context = GetDatabaseContext();
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new ScannerController(new Mock<IOrderService>().Object, context);

            // Act
            var result = await controller.Scan(order.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            Assert.Equal(order.Id, model.CurrentOrder?.Id);
        }

        [Fact]
        public async Task ScanProduct_ValidProduct_RemovesFromRemainingAndReturnsView()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Id = 1, Name = "P1", Brand = "B1", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now };
            order.OrderItems.Add(new OrderItem { ProductId = 1, Quantity = 1, UnitPrice = 10, TotalPrice = 10 });
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new ScannerController(new Mock<IOrderService>().Object, context);

            // Act
            var result = await controller.ScanProduct(order.Id, 1, "1", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            Assert.Empty(model.RemainingProductIds);
        }

        [Fact]
        public async Task MarkOrderScanned_ValidInput_UpdatesStatusAndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var packer = new Employee { FullName = "Packer", Position = "Packer", Phone = "123", IsActive = true };
            context.Employees.Add(packer);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var controller = new ScannerController(new Mock<IOrderService>().Object, context);
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = await controller.MarkOrderScanned(order.Id, "", packer.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var updatedOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Scanned, updatedOrder.OrderStatus);
        }
    }
}
