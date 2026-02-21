using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using StockFlowPro.Services;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Services
{
    public class OrderServiceMoreTests
    {
        [Fact]
        public async Task CreateOrderAsync_Throws_When_Product_NotFound()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            ctx.Facilities.Add(Builders.BuildFacility(1));
            await ctx.SaveChangesAsync();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);
            await Assert.ThrowsAsync<Exception>(() =>
                svc.CreateOrderAsync(1, new List<OrderItemDto> { new() { ProductId = 123, Quantity = 1 } }));
        }
    }
}
