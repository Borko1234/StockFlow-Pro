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
        public async Task Index_With_Invalid_Order_Shows_Error()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var orderService = new Mock<IOrderService>(MockBehavior.Strict);
            var controller = new ScannerController(ctx, orderService.Object);
            var result = await controller.Index(999);
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ScannerViewModel>(view.Model);
            Assert.Contains("Order not found", model.ErrorMessage ?? "");
        }

        [Fact]
        public async Task Products_Returns_View_With_Model()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var order = new Order { Id = 2, FacilityId = 1 };
            ctx.Orders.Add(order);
            await ctx.SaveChangesAsync();
            var controller = new ScannerController(ctx, Mock.Of<IOrderService>());
            var result = await controller.Products(2, null);
            var view = Assert.IsType<ViewResult>(result);
            Assert.NotNull(view.Model);
        }

        [Fact]
        public async Task ScanItem_Invalid_Barcode_Returns_Error_Json()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var controller = new ScannerController(ctx, Mock.Of<IOrderService>());
            var result = await controller.ScanItem(1, "abc");
            var json = Assert.IsType<JsonResult>(result);
            var value = json.Value!;
            var prop = value.GetType().GetProperty("success");
            Assert.NotNull(prop);
            Assert.False((bool)prop!.GetValue(value)!);
        }

        [Fact]
        public async Task ScanItem_Found_Returns_Product_Info()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var order = new Order { Id = 1, FacilityId = 1 };
            var product = Builders.BuildProduct(7, name: "P7");
            ctx.Orders.Add(order);
            ctx.Products.Add(product);
            ctx.OrderItems.Add(new OrderItem { OrderId = order.Id, ProductId = product.Id, Quantity = 2, UnitPrice = 1, TotalPrice = 2 });
            await ctx.SaveChangesAsync();

            var controller = new ScannerController(ctx, Mock.Of<IOrderService>());
            var result = await controller.ScanItem(order.Id, product.Id.ToString());
            var json = Assert.IsType<JsonResult>(result);
            var value = json.Value!;
            var successProp = value.GetType().GetProperty("success");
            Assert.True((bool)successProp!.GetValue(value)!);
        }

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
            var value = json.Value!;
            var prop = value.GetType().GetProperty("success");
            Assert.NotNull(prop);
            Assert.True((bool)prop!.GetValue(value)!);
        }
    }
}
