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
using StockFlowPro.ViewModels;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class ScannerControllerTests
    {
        [Fact]
        public async Task Assign_NonPending_Order_Shows_Error_In_View()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>(MockBehavior.Strict);
            var controller = new ScannerController(ctx, orderService.Object);

            var order = new Order { Id = 1, FacilityId = 1, OrderStatus = OrderStatus.Scanned };
            ctx.Orders.Add(order);
            ctx.Employees.Add(new Employee { Id = 10, FullName = "Emp", Position = "Scanner", Phone = "000", IsActive = true, CreatedAt = System.DateTime.UtcNow });
            await ctx.SaveChangesAsync();

            var model = new ScannerViewModel { OrderId = 1, SelectedEmployeeId = 10 };

            var result = await controller.Assign(model);

            var view = Assert.IsType<ViewResult>(result);
            var returnedModel = Assert.IsType<ScannerViewModel>(view.Model);
            Assert.Contains("Only Pending orders can be scanned", returnedModel.ErrorMessage);
        }

        [Fact]
        public async Task CompleteScan_Success_Returns_Json_Success()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>();
            orderService.Setup(s => s.UpdateOrderStatusAsync(1, OrderStatus.Scanned)).ReturnsAsync(true);
            var controller = new ScannerController(ctx, orderService.Object);

            var order = new Order { Id = 1, FacilityId = 1, OrderStatus = OrderStatus.Pending };
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();

            var result = await controller.CompleteScan(1);

            var json = Assert.IsType<JsonResult>(result);
            var dict = Assert.IsType<Dictionary<string, object>>(json.Value!);
            Assert.True((bool)dict["success"]);
        }
    }
}
