using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.ViewModels.Admin;
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
            context.Products.Add(new Product { Name = "P1", Brand = "B1", Price = 10, QuantityInStock = 100 });
            context.Products.Add(new Product { Name = "P2", Brand = "B2", Price = 20, QuantityInStock = 200 });
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProductListViewModel>(viewResult.Model);
            Assert.Equal(2, model.Products.Count);
        }

        [Fact]
        public async Task Create_AddsProduct_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminProductsController(context);
            var product = new Product { Name = "New Product", Brand = "Brand", Price = 10, QuantityInStock = 100 };

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
            var product = new Product { Name = "Old Name", Brand = "Brand", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);
            product.Name = "Updated Name";

            // Act
            var result = await controller.Edit(product.Id, product);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Updated Name", context.Products.First().Name);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesProduct_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Name = "To Delete", Brand = "Brand", Price = 10, QuantityInStock = 100 };
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

        [Fact]
        public async Task LookupByProductId_ReturnsJsonResult_WithFoundTrue()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Name = "P1", Brand = "B1", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);

            // Act
            var result = await controller.LookupByProductId(product.Id.ToString());

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var data = jsonResult.Value;
            var foundProp = data.GetType().GetProperty("found").GetValue(data);
            Assert.True((bool)foundProp);
        }

        [Fact]
        public async Task QuickUpdateQuantity_UpdatesStock_AndReturnsJson()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Name = "P1", Brand = "B1", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);

            // Act
            var result = await controller.QuickUpdateQuantity(product.Id, 10);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var updatedProduct = await context.Products.FindAsync(product.Id);
            Assert.Equal(110, updatedProduct.QuantityInStock);
        }

        [Fact]
        public async Task ProductExists_ReturnsTrue_WhenProductExists()
        {
            // Arrange
            var context = GetDatabaseContext();
            var product = new Product { Name = "P1", Brand = "B1", Price = 10, QuantityInStock = 100 };
            context.Products.Add(product);
            await context.SaveChangesAsync();

            var controller = new AdminProductsController(context);

            // Act
            var method = typeof(AdminProductsController).GetMethod("ProductExists", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var exists = (bool)method.Invoke(controller, new object[] { product.Id });

            // Assert
            Assert.True(exists);
        }

        [Fact]
        public async Task Edit_ReturnsNotFound_WhenIdMismatch()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminProductsController(context);
            var product = new Product { Id = 1, Name = "P1" };

            // Act
            var result = await controller.Edit(2, product);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenIdNull()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminProductsController(context);

            // Act
            var result = await controller.Delete(null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
