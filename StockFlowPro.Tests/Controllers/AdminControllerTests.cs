using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Models;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminControllerTests
    {
        [Fact]
        public async Task Index_Computes_Basic_Metrics_And_Returns_View()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            // seed products and orders
            var p1 = Builders.BuildProduct(1, price: 10m);
            var p2 = Builders.BuildProduct(2, price: 5m);
            ctx.Products.AddRange(p1, p2);
            var o1 = new Order { FacilityId = 1, TotalAmount = 20m };
            var o2 = new Order { FacilityId = 1, TotalAmount = 10m };
            ctx.Orders.AddRange(o1, o2);
            ctx.OrderItems.AddRange(
                new OrderItem { Order = o1, Product = p1, Quantity = 1, UnitPrice = 10m, TotalPrice = 10m },
                new OrderItem { Order = o1, Product = p2, Quantity = 2, UnitPrice = 5m, TotalPrice = 10m },
                new OrderItem { Order = o2, Product = p2, Quantity = 2, UnitPrice = 5m, TotalPrice = 10m }
            );
            await ctx.SaveChangesAsync();

            var controller = new AdminController(ctx);
            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            Assert.NotNull(view.Model);
        }

        [Fact]
        public async Task AddStockToAll_Increments_All_Products_By_100()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var p1 = Builders.BuildProduct(1, price: 10m, qty: 5);
            var p2 = Builders.BuildProduct(2, price: 5m, qty: 0);
            ctx.Products.AddRange(p1, p2);
            await ctx.SaveChangesAsync();

            var controller = new AdminController(ctx);
            var result = await controller.AddStockToAll();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            var reloaded = await ctx.Products.OrderBy(p => p.Id).ToListAsync();
            Assert.Equal(105, reloaded[0].QuantityInStock);
            Assert.Equal(100, reloaded[1].QuantityInStock);
        }
    }
}
