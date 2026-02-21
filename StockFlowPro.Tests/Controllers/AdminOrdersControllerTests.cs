using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminOrdersControllerTests
    {
        [Fact]
        public async Task UpdateStatus_To_Pending_Clears_PreparedBy_And_Returns_Success()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>();
            orderService.Setup(s => s.UpdateOrderStatusAsync(1, OrderStatus.Pending)).ReturnsAsync(true);
            var controller = new AdminOrdersController(ctx, orderService.Object);

            var order = new Order
            {
                Id = 1,
                FacilityId = 1,
                OrderStatus = OrderStatus.Scanned,
                OrderProcessing = new OrderProcessing { PreparedByEmployeeId = 10 }
            };
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateStatus(1, OrderStatus.Pending);

            var json = Assert.IsType<JsonResult>(result);
            var dict = Assert.IsType<Dictionary<string, object>>(json.Value!);
            Assert.True((bool)dict["success"]);
            Assert.Null(order.OrderProcessing!.PreparedByEmployeeId);
        }

        [Fact]
        public async Task MarkDelivered_From_Scanned_Records_Audit_And_Redirects()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>();
            orderService.Setup(s => s.UpdateOrderStatusAsync(1, OrderStatus.Delivered)).ReturnsAsync(true);
            var controller = new AdminOrdersController(ctx, orderService.Object);

            ctx.Orders.Add(new Order { Id = 1, FacilityId = 1, OrderStatus = OrderStatus.Scanned });
            await ctx.SaveChangesAsync();

            var result = await controller.MarkDelivered(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Single(ctx.OrderStatusAuditLogs);
        }
    }
}
