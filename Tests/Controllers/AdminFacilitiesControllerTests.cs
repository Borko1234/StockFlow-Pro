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
    public class AdminFacilitiesControllerTests
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
        public async Task Index_ReturnsViewResult_WithFacilities()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Facilities.Add(new Facility { Name = "F1", Address = "A1", Phone = "123", RepresentativeName = "R1" });
            context.Facilities.Add(new Facility { Name = "F2", Address = "A2", Phone = "456", RepresentativeName = "R2" });
            await context.SaveChangesAsync();

            var controller = new AdminFacilitiesController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Facility>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Create_AddsFacility_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminFacilitiesController(context);
            var facility = new Facility { Name = "New Facility", Address = "Address", Phone = "123", RepresentativeName = "Rep" };

            // Act
            var result = await controller.Create(facility);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Facilities.Count());
            Assert.Equal("New Facility", context.Facilities.First().Name);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesFacility_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "To Delete", Address = "Address", Phone = "123", RepresentativeName = "Rep" };
            context.Facilities.Add(facility);
            await context.SaveChangesAsync();

            var controller = new AdminFacilitiesController(context);

            // Act
            var result = await controller.DeleteConfirmed(facility.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(0, context.Facilities.Count());
        }

        [Fact]
        public async Task Edit_UpdatesFacility_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "Old Name", Address = "A1", Phone = "123", RepresentativeName = "Rep" };
            context.Facilities.Add(facility);
            await context.SaveChangesAsync();

            var controller = new AdminFacilitiesController(context);
            facility.Name = "Updated Name";

            // Act
            var result = await controller.Edit(facility.Id, facility);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal("Updated Name", context.Facilities.First().Name);
        }
    }
}
