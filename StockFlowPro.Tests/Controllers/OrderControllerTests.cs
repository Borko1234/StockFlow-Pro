using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Services;
using StockFlowPro.Tests.TestUtils;
using StockFlowPro.ViewModels.Order;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class OrderControllerTests
    {
        [Fact]
        public async Task Create_Post_Valid_Redirects_To_Home()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            ctx.Facilities.Add(Builders.BuildFacility(1));
            ctx.Products.Add(Builders.BuildProduct(1));
            await ctx.SaveChangesAsync();

            var svc = new Mock<IOrderService>();
            svc.Setup(s => s.CreateOrderAsync(1, It.IsAny<List<OrderItemDto>>()))
               .ReturnsAsync(new StockFlowPro.Models.Order { FacilityId = 1 });
            var controller = new OrderController(svc.Object, ctx, NullLogger<OrderController>.Instance);

            var model = new CreateOrderViewModel
            {
                FacilityId = 1,
                OrderItems = new List<OrderItemViewModel> { new() { ProductId = 1, Quantity = 2 } }
            };
            var result = await controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Create_Post_No_Valid_Items_Returns_View_With_Error()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            ctx.Facilities.Add(Builders.BuildFacility(1));
            await ctx.SaveChangesAsync();

            var svc = new Mock<IOrderService>(MockBehavior.Strict);
            var controller = new OrderController(svc.Object, ctx, NullLogger<OrderController>.Instance);
            var model = new CreateOrderViewModel
            {
                FacilityId = 1,
                OrderItems = new List<OrderItemViewModel> { new() { ProductId = 0, Quantity = 0 } }
            };
            var result = await controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }
        [Fact]
        public async Task Create_Get_ReturnsView_WithPopulatedLists()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            ctx.Facilities.Add(Builders.BuildFacility(1));
            ctx.Products.Add(Builders.BuildProduct(1));
            await ctx.SaveChangesAsync();

            var orderService = new Mock<IOrderService>(MockBehavior.Strict);
            var controller = new OrderController(orderService.Object, ctx, NullLogger<OrderController>.Instance);

            // Act
            var result = await controller.Create();

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CreateOrderViewModel>(view.Model);
            Assert.NotNull(model.Facilities);
            Assert.NotNull(model.Products);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView_WithModelErrors()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>(MockBehavior.Strict);
            var controller = new OrderController(orderService.Object, ctx, NullLogger<OrderController>.Instance);
            controller.ModelState.AddModelError("FacilityId", "Required");

            var model = new CreateOrderViewModel
            {
                FacilityId = 0,
                OrderItems = new List<OrderItemViewModel>()
            };

            // Act
            var result = await controller.Create(model);

            // Assert
            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
            Assert.IsType<CreateOrderViewModel>(view.Model);
        }
    }
}
