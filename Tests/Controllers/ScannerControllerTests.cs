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
        public async Task Scan_WithValidOrderId_ReturnsViewWithOrder()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Test Facility", Address = "Test", Phone = "123", RepresentativeName = "Test" };
            context.Facilities.Add(facility);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility };
            context.Orders.Add(order);
            context.Employees.Add(new Employee { FullName = "Packer 1", Position = "Packer", Phone = "123" });
            await context.SaveChangesAsync();

            var mockOrderService = new Mock<IOrderService>();
            var controller = new ScannerController(mockOrderService.Object, context);

            // Act
            var result = await controller.Scan(order.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            Assert.NotNull(model.CurrentOrder);
            Assert.Equal(order.Id, model.CurrentOrder.Id);
        }

        [Fact]
        public async Task Scan_WithInvalidOrderId_ReturnsError()
        {
            // Arrange
            var context = GetDatabaseContext();
            var mockOrderService = new Mock<IOrderService>();
            var controller = new ScannerController(mockOrderService.Object, context);

            // Act
            var result = await controller.Scan(999);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            Assert.NotNull(model.ErrorMessage);
            Assert.Contains("not found", model.ErrorMessage);
        }

        [Fact]
        public async Task ScanProduct_WithValidProduct_UpdatesRemainingItems()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Test Facility", Address = "Test", Phone = "123", RepresentativeName = "Test" };
            context.Facilities.Add(facility);
            var product = new Product { Name = "Test Product", Brand = "Brand", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility };
            context.Orders.Add(order);
            context.OrderItems.Add(new OrderItem { Order = order, Product = product, Quantity = 2 });
            context.Employees.Add(new Employee { FullName = "Packer 1", Position = "Packer", Phone = "123" });
            await context.SaveChangesAsync();

            var mockOrderService = new Mock<IOrderService>();
            var controller = new ScannerController(mockOrderService.Object, context);

            // Simulate initial scan to get remaining IDs
            // But here we can just construct the remaining IDs string manually
            // Product ID 1 repeated twice: "1,1"
            string remainingIds = $"{product.Id},{product.Id}";

            // Act
            var result = await controller.ScanProduct(order.Id, product.Id, remainingIds, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            
            // Should have 1 remaining item
            Assert.Equal(1, model.RemainingItems);
            Assert.True(model.Items.Count(i => i.IsScanned) == 1);
        }

        [Fact]
        public async Task ScanProduct_WithInvalidProduct_ReturnsError()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "1" };
            context.Facilities.Add(facility);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var mockOrderService = new Mock<IOrderService>();
            var controller = new ScannerController(mockOrderService.Object, context);

            // Act
            var result = await controller.ScanProduct(order.Id, 999, "", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(viewResult.Model);
            // The actual error message is "Product ID 999 is not part of the remaining items to scan."
            Assert.Contains("not part of the remaining", model.ErrorMessage);
        }

        [Fact]
        public async Task MarkOrderScanned_UpdatesStatus_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "1" };
            context.Facilities.Add(facility);
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility };
            context.Orders.Add(order);
            var packer = new Employee { FullName = "Packer", Position = "Packer" };
            context.Employees.Add(packer);
            context.OrderProcessings.Add(new OrderProcessing { OrderId = order.Id });
            await context.SaveChangesAsync();

            var mockOrderService = new Mock<IOrderService>();
            mockOrderService.Setup(s => s.UpdateOrderStatusAsync(order.Id, OrderStatus.Scanned)).ReturnsAsync(true);

            var controller = new ScannerController(mockOrderService.Object, context)
            {
                TempData = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>().Object
            };

            // Act
            var result = await controller.MarkOrderScanned(order.Id, "", packer.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            
            var processedOrder = await context.OrderProcessings.FirstOrDefaultAsync(op => op.OrderId == order.Id);
            Assert.Equal(packer.Id, processedOrder.PreparedByEmployeeId);
        }
    }
}
