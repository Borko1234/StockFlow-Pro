using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminProductsControllerTests
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
        public async Task Index_ReturnsViewResult_WithProducts()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Products.Add(new Product { Name = "Product 1", Brand = "Brand 1", Price = 10 });
            context.Products.Add(new Product { Name = "Product 2", Brand = "Brand 2", Price = 20 });
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Product>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Create_AddsProduct_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminProductsController(context);
            var product = new Product { Name = "New Product", Brand = "New Brand", Price = 30 };

            // Act
            var result = await controller.Create(product);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Products.Count());
            Assert.Equal("New Product", context.Products.First().Name);
        }

        [Fact]
        public async Task Edit_UpdatesProduct_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Name = "Old Product", Brand = "Old Brand", Price = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);
            product.Name = "Updated Product";

            // Act
            var result = await controller.Edit(product.Id, product);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Updated Product", context.Products.First().Name);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesProduct_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Name = "To Delete", Brand = "Brand", Price = 10 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);

            // Act
            var result = await controller.DeleteConfirmed(product.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(0, context.Products.Count());
        }
    }
}
