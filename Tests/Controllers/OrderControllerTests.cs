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
using StockFlowPro.Services;
using StockFlowPro.ViewModels.Order;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class OrderControllerTests
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
        public async Task Create_ReturnsView_WithPopulatedLists()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Facilities.Add(new Facility { Name = "F1", Address = "A1", Phone = "1" });
            context.Products.Add(new Product { Name = "P1", Brand = "B1", Price = 10 });
            await context.SaveChangesAsync();

            var mockOrderService = new Mock<IOrderService>();
            var controller = new OrderController(mockOrderService.Object, context);

            // Act
            var result = await controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateOrderViewModel>(viewResult.ViewData.Model);
            Assert.NotNull(model.Facilities);
            Assert.NotEmpty(model.Facilities);
            Assert.NotNull(model.Products);
            Assert.NotEmpty(model.Products);
        }

        [Fact]
        public async Task Create_Post_Redirects_WhenValid()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Facilities.Add(new Facility { Id = 1, Name = "F1", Address = "A1", Phone = "1", IsActive = true });
            await context.SaveChangesAsync();

            var mockOrderService = new Mock<IOrderService>();
            mockOrderService.Setup(s => s.CreateOrderAsync(It.IsAny<int>(), It.IsAny<List<OrderItemDto>>()))
                .ReturnsAsync(new Order { Id = 1 });

            var controller = new OrderController(mockOrderService.Object, context);
            var model = new CreateOrderViewModel
            {
                FacilityId = 1,
                OrderItems = new List<OrderItemViewModel>
                {
                    new OrderItemViewModel { ProductId = 1, Quantity = 5 }
                }
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Home", redirectToActionResult.ControllerName);
        }
    }
}
