using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Services
{
    public class OrderServiceTests
    {
        [Fact]
        public async Task CreateOrderAsync_Throws_When_No_Items()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var logger = NullLogger<OrderService>.Instance;
            var svc = new OrderService(ctx, logger);

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateOrderAsync(1, new List<OrderItemDto>()));
        }

        [Fact]
        public async Task CreateOrderAsync_Throws_When_Facility_NotFound()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var logger = NullLogger<OrderService>.Instance;
            var svc = new OrderService(ctx, logger);
            var items = new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } };
            ctx.Products.Add(Builders.BuildProduct(1));
            await ctx.SaveChangesAsync();

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateOrderAsync(99, items));
        }

        [Fact]
        public async Task CreateOrderAsync_Creates_Order_With_Total_And_Processing()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var logger = NullLogger<OrderService>.Instance;
            var svc = new OrderService(ctx, logger);

            ctx.Facilities.Add(Builders.BuildFacility(1, isActive: true));
            ctx.Products.AddRange(
                Builders.BuildProduct(1, price: 5m),
                Builders.BuildProduct(2, price: 10m)
            );
            await ctx.SaveChangesAsync();

            var items = new List<OrderItemDto>
            {
                new() { ProductId = 1, Quantity = 2 }, // 10
                new() { ProductId = 2, Quantity = 3 }  // 30 -> total 40
            };

            // Act
            var order = await svc.CreateOrderAsync(1, items);

            // Assert
            Assert.NotNull(order);
            Assert.True(order.Id > 0);
            Assert.Equal(40m, order.TotalAmount);
            Assert.Equal(OrderStatus.Pending, order.OrderStatus);
            Assert.NotNull(order.OrderProcessing);
            Assert.Equal(2, order.OrderItems.Count);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ReturnsFalse_When_Order_NotFound()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);

            // Act
            var result = await svc.UpdateOrderStatusAsync(123, OrderStatus.Scanned);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_Decrements_Stock_On_Pending_To_Scanned()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);
            var facility = Builders.BuildFacility(1);
            var product = Builders.BuildProduct(1, price: 10m, qty: 20);
            ctx.Facilities.Add(facility);
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(1, new List<OrderItemDto> { new() { ProductId = 1, Quantity = 5 } });
            var beforeQty = product.QuantityInStock;

            // Act
            var result = await svc.UpdateOrderStatusAsync(order.Id, OrderStatus.Scanned);

            // Assert
            Assert.True(result);
            Assert.Equal(beforeQty - 5, product.QuantityInStock);
            Assert.Equal(OrderStatus.Scanned, order.OrderStatus);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_Throws_When_Insufficient_Stock()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);
            ctx.Facilities.Add(Builders.BuildFacility(1));
            ctx.Products.Add(Builders.BuildProduct(1, qty: 3));
            await ctx.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(1, new List<OrderItemDto> { new() { ProductId = 1, Quantity = 5 } });

            // Act + Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateOrderStatusAsync(order.Id, OrderStatus.Scanned));
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_No_Decrement_If_Not_Pending_To_Scanned_Or_Delivered()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);
            ctx.Facilities.Add(Builders.BuildFacility(1));
            var product = Builders.BuildProduct(1, qty: 50);
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(1, new List<OrderItemDto> { new() { ProductId = 1, Quantity = 10 } });
            order.OrderStatus = OrderStatus.Scanned; // move out of Pending
            await ctx.SaveChangesAsync();
            var before = product.QuantityInStock;

            // Act
            var result = await svc.UpdateOrderStatusAsync(order.Id, OrderStatus.Delivered);

            // Assert
            Assert.True(result);
            Assert.Equal(before, product.QuantityInStock);
            Assert.Equal(OrderStatus.Delivered, order.OrderStatus);
        }

        [Fact]
        public async Task ScanOrderAsync_ReturnsFalse_When_NotFound()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);

            // Act
            var result = await svc.ScanOrderAsync(42);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ScanOrderAsync_Transitions_Pending_To_Scanned()
        {
            // Arrange
            using var ctx = TestDbContextFactory.CreateContext();
            var svc = new OrderService(ctx, NullLogger<OrderService>.Instance);
            ctx.Facilities.Add(Builders.BuildFacility(1));
            ctx.Products.Add(Builders.BuildProduct(1, qty: 100));
            await ctx.SaveChangesAsync();

            var order = await svc.CreateOrderAsync(1, new List<OrderItemDto> { new() { ProductId = 1, Quantity = 1 } });
            Assert.Equal(OrderStatus.Pending, order.OrderStatus);

            // Act
            var ok = await svc.ScanOrderAsync(order.Id);

            // Assert
            Assert.True(ok);
            Assert.Equal(OrderStatus.Scanned, order.OrderStatus);
        }
    }
}
