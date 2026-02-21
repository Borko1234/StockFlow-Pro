using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "test-id"),
                        new Claim(ClaimTypes.Name, "test-user")
                    }, "TestAuth"))
                }
            };

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
            var value = json.Value!;
            var prop = value.GetType().GetProperty("success");
            Assert.NotNull(prop);
            Assert.True((bool)prop!.GetValue(value)!);
            Assert.Null(order.OrderProcessing!.PreparedByEmployeeId);
        }

        [Fact]
        public async Task MarkDelivered_From_Scanned_Records_Audit_And_Redirects()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>();
            orderService.Setup(s => s.UpdateOrderStatusAsync(1, OrderStatus.Delivered)).ReturnsAsync(true);
            var controller = new AdminOrdersController(ctx, orderService.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "test-id"),
                        new Claim(ClaimTypes.Name, "test-user")
                    }, "TestAuth"))
                }
            };

            ctx.Orders.Add(new Order { Id = 1, FacilityId = 1, OrderStatus = OrderStatus.Scanned });
            await ctx.SaveChangesAsync();

            var result = await controller.MarkDelivered(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Single(ctx.OrderStatusAuditLogs);
        }

        [Fact]
        public async Task UpdateStatus_AlreadySet_Returns_Error_Message()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>();
            var controller = new AdminOrdersController(ctx, orderService.Object);
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
                {
                    User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
                    {
                        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "id")
                    }, "TestAuth"))
                }
            };

            ctx.Orders.Add(new Order { Id = 5, FacilityId = 1, OrderStatus = OrderStatus.Pending, OrderProcessing = new OrderProcessing() });
            await ctx.SaveChangesAsync();

            var result = await controller.UpdateStatus(5, OrderStatus.Pending);
            var json = Assert.IsType<JsonResult>(result);
            var v = json.Value!;
            var successProp = v.GetType().GetProperty("success");
            Assert.False((bool)successProp!.GetValue(v)!);
        }
    }
}
