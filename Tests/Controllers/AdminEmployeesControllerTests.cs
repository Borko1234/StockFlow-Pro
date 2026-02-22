using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminEmployeesControllerTests
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

        private Mock<UserManager<IdentityUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            return new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Index_ReturnsView_WithEmployees()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Employees.Add(new Employee { FullName = "Emp 1", Position = "Pos 1", Phone = "1" });
            await context.SaveChangesAsync();

            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Employee>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_AddsEmployee_WithoutLogin()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);
            var employee = new Employee { FullName = "New Emp", Position = "Packer", Phone = "123" };

            // Act
            var result = await controller.Create(employee, null, null, false);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Employees.Count());
        }

        [Fact]
        public async Task Delete_ReturnsView_WithEmployee()
        {
            // Arrange
            var context = GetDatabaseContext();
            var employee = new Employee { FullName = "To Delete", Position = "Pos", Phone = "1" };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);

            // Act
            var result = await controller.Delete(employee.Id);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Employee>(viewResult.ViewData.Model);
            Assert.Equal(employee.Id, model.Id);
        }

        [Fact]
        public async Task Create_Post_WithLogin_InvalidData_ReturnsView()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);
            controller.ModelState.AddModelError("Error", "Sample Error");
            var employee = new Employee { FullName = "New Emp" };

            // Act
            var result = await controller.Create(employee, "email@test.com", "pass", true);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(employee, viewResult.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_DeletesLinkedUser()
        {
            // Arrange
            var context = GetDatabaseContext();
            var user = new IdentityUser { Id = "user1", UserName = "testuser" };
            var employee = new Employee { FullName = "Emp User", UserId = "user1" };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            var mockUserManager = GetMockUserManager();
            mockUserManager.Setup(u => u.FindByIdAsync("user1")).ReturnsAsync(user);
            mockUserManager.Setup(u => u.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

            var controller = new AdminEmployeesController(context, mockUserManager.Object);

            // Act
            var result = await controller.DeleteConfirmed(employee.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            mockUserManager.Verify(u => u.DeleteAsync(user), Times.Once);
            Assert.Equal(0, context.Employees.Count());
        }
    }
}
