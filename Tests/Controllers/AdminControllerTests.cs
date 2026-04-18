using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.ViewModels.Admin;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminControllerTests
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
        public async Task Index_ReturnsDashboardViewModel_WithCorrectStats()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "1" };
            context.Facilities.Add(facility);
            
            var product = new Product { Name = "P1", Brand = "B1", Price = 100, CostPrice = 50 };
            context.Products.Add(product);

            var order1 = new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 };
            var order2 = new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 200 };
            context.Orders.AddRange(order1, order2);

            context.OrderItems.Add(new OrderItem { Order = order2, Product = product, Quantity = 2, TotalPrice = 200 });

            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminDashboardViewModel>(viewResult.Model);
            Assert.Equal(2, model.TotalOrders);
            Assert.Equal(1, model.PendingOrders);
            Assert.Equal(200, model.TotalRevenue);
            Assert.Single(model.LowStockProducts);
            Assert.Equal("P1", model.LowStockProducts.First().Name);
        }

        [Fact]
        public async Task Index_CalculatesTopProducts_AndMonthlyProfits()
        {
            // Arrange
            var context = GetDatabaseContext();
            var p1 = new Product { Name = "P1", Brand = "B1", Price = 20, CostPrice = 10, QuantityInStock = 100 };
            var p2 = new Product { Name = "P2", Brand = "B2", Price = 30, CostPrice = 15, QuantityInStock = 100 };
            context.Products.AddRange(p1, p2);

            var o1 = new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Today, TotalAmount = 100 };
            o1.OrderItems.Add(new OrderItem { Product = p1, Quantity = 5, UnitPrice = 20, TotalPrice = 100 });
            
            var o2 = new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Today.AddMonths(-1), TotalAmount = 60 };
            o2.OrderItems.Add(new OrderItem { Product = p2, Quantity = 2, UnitPrice = 30, TotalPrice = 60 });
            
            context.Orders.AddRange(o1, o2);
            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminDashboardViewModel>(viewResult.Model);
            
            Assert.NotEmpty(model.TopProducts);
            Assert.Equal("P1", model.TopProducts.First().ProductName);
            Assert.Equal(5, model.TopProducts.First().UnitsSold);
            Assert.Equal(100, model.TopProducts.First().Revenue);
            Assert.Equal(50, model.TopProducts.First().ProfitMargin); // (100 - 50) / 100 * 100
            Assert.Equal(5.0 / 7.0 * 100.0, model.TopProducts.First().PercentageOfTotal, 5);
            
            Assert.Equal(6, model.MonthlyProfits.Count);
            var currentMonth = model.MonthlyProfits.Last();
            Assert.Equal(100, currentMonth.Revenue);
            Assert.Equal(50, currentMonth.Profit); // 100 - (5 * 10)
        }

        [Fact]
        public async Task LowStockThreshold_Works()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Products.Add(new Product { Name = "Low Stock", Brand = "B", Price = 10, QuantityInStock = 2, MinimumStockLevel = 5 });
            context.Products.Add(new Product { Name = "High Stock", Brand = "B", Price = 10, QuantityInStock = 20, MinimumStockLevel = 5 });
            await context.SaveChangesAsync();

            var controller = new AdminController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AdminDashboardViewModel>(viewResult.Model);
            Assert.Single(model.LowStockProducts);
            Assert.Equal("Low Stock", model.LowStockProducts.First().Name);
        }
    }
}
