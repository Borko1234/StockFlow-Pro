using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using Xunit;

namespace StockFlowPro.Tests.Services
{
    public class OrderServiceTests
    {
        private FoodieDbContext GetDatabaseContext()
        {
            var options = new DbContextOptionsBuilder<FoodieDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var databaseContext = new FoodieDbContext(options);
            databaseContext.Database.EnsureCreated();
            return databaseContext;
        }

        [Fact]
        public async Task CreateOrderAsync_CreatesOrder_WithValidItems()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" };
            context.Facilities.Add(facility);
            var product = new Product { Name = "Test", Brand = "Brand", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var service = new OrderService(context);
            var items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = product.Id, Quantity = 5 }
            };

            // Act
            var order = await service.CreateOrderAsync(facility.Id, items);

            // Assert
            Assert.NotNull(order);
            Assert.Equal(OrderStatus.Created, order.OrderStatus);
            Assert.Single(order.OrderItems);
            Assert.Equal(50, order.TotalAmount); // 5 * 10
            Assert.Equal(product.Id, order.OrderItems.First().ProductId);
        }

        [Fact]
        public async Task CreateOrderAsync_ThrowsException_WhenProductNotFound()
        {
            // Arrange
            var context = GetDatabaseContext();
            var service = new OrderService(context);
            var items = new List<OrderItemDto>
            {
                new OrderItemDto { ProductId = 999, Quantity = 5 }
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.CreateOrderAsync(1, items)); // Facility ID doesn't matter much here
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_UpdatesStatus()
        {
            // Arrange
            var context = GetDatabaseContext();
            var order = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" } };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            // Act
            var result = await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Prepared);

            // Assert
            Assert.True(result);
            var updatedOrder = await context.Orders.FindAsync(order.Id);
            Assert.Equal(OrderStatus.Prepared, updatedOrder.OrderStatus);
        }

        [Fact]
        public async Task UpdateOrderStatusAsync_ClearsPreparedBy_WhenStatusIsCreated()
        {
            // Arrange
            var context = GetDatabaseContext();
            var employee = new Employee { FullName = "Packer", Position = "Packer", Phone = "123" };
            context.Employees.Add(employee);
            var order = new Order 
            { 
                OrderStatus = OrderStatus.Prepared, 
                CreatedAt = DateTime.Now, 
                Facility = new Facility { Name = "Test", Address = "Test", Phone = "123", RepresentativeName = "Test" },
                OrderProcessing = new OrderProcessing { PreparedByEmployeeId = employee.Id }
            };
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var service = new OrderService(context);

            // Act
            await service.UpdateOrderStatusAsync(order.Id, OrderStatus.Created);

            // Assert
            var updatedOrder = await context.Orders.Include(o => o.OrderProcessing).FirstOrDefaultAsync(o => o.Id == order.Id);
            Assert.Equal(OrderStatus.Created, updatedOrder.OrderStatus);
            Assert.Null(updatedOrder.OrderProcessing.PreparedByEmployeeId);
        }
    }
}
