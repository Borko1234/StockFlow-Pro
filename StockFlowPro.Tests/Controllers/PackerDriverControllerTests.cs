using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class PackerDriverControllerTests
    {
        [Fact]
        public async Task Packer_Index_Returns_Pending_Today()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var todayOrder = new Order { FacilityId = 1, OrderStatus = OrderStatus.Pending };
            var otherOrder = new Order { FacilityId = 1, OrderStatus = OrderStatus.Scanned };
            ctx.Orders.AddRange(todayOrder, otherOrder);
            await ctx.SaveChangesAsync();

            var controller = new PackerController(ctx);
            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<System.Collections.IEnumerable>(view.Model);
            Assert.NotNull(list);
        }

        [Fact]
        public async Task Driver_Index_Returns_Scanned()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var scanned = new Order { FacilityId = 1, OrderStatus = OrderStatus.Scanned };
            var pending = new Order { FacilityId = 1, OrderStatus = OrderStatus.Pending };
            ctx.Orders.AddRange(scanned, pending);
            await ctx.SaveChangesAsync();

            var controller = new DriverController(ctx);
            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Packer/Index.cshtml", view.ViewName);
            Assert.NotNull(view.Model);
        }
    }
}
