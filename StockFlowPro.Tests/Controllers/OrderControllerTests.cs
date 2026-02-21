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
