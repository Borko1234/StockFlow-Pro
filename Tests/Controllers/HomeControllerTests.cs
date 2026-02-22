using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.ViewModels;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class HomeControllerTests
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
        public async Task Index_ReturnsView_WithStats()
        {
            // Arrange
            var context = GetDatabaseContext();
            var facility = new Facility { Name = "F1", Address = "A1", Phone = "1" };
            context.Facilities.Add(facility);
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Created, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 100 });
            context.Orders.Add(new Order { OrderStatus = OrderStatus.Delivered, CreatedAt = DateTime.Now, Facility = facility, TotalAmount = 200 });
            await context.SaveChangesAsync();

            var store = new Mock<IUserStore<IdentityUser>>();
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new IdentityUser { UserName = "TestUser" });

            var controller = new HomeController(context, userManager.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(viewResult.ViewData.Model);
            Assert.Equal(2, model.TodaysOrders.Count);
        }
    }
}
