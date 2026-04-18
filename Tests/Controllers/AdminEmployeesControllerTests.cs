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
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            return userManager;
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithEmployees()
        {
            // Arrange
            var context = GetDatabaseContext();
            context.Employees.Add(new Employee { FullName = "E1", Position = "P1", Phone = "123" });
            context.Employees.Add(new Employee { FullName = "E2", Position = "P2", Phone = "456" });
            await context.SaveChangesAsync();

            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<Employee>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Create_AddsEmployee_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var userManagerMock = GetMockUserManager();
            userManagerMock.Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new AdminEmployeesController(context, userManagerMock.Object);
            var employee = new Employee { FullName = "New Employee", Position = "Scanner", Phone = "789" };

            // Act
            var result = await controller.Create(employee, "test@test.com", "Password123!", true);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Employees.Count());
            Assert.Equal("New Employee", context.Employees.First().FullName);
        }

        [Fact]
        public async Task DeleteConfirmed_RemovesEmployee_AndRedirects()
        {
            // Arrange
            var context = GetDatabaseContext();
            var employee = new Employee { FullName = "To Delete", Position = "P1", Phone = "123" };
            context.Employees.Add(employee);
            await context.SaveChangesAsync();

            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);

            // Act
            var result = await controller.DeleteConfirmed(employee.Id);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(0, context.Employees.Count());
        }

        [Fact]
        public async Task Create_ReturnsView_WhenModelStateInvalid()
        {
            // Arrange
            var context = GetDatabaseContext();
            var controller = new AdminEmployeesController(context, GetMockUserManager().Object);
            controller.ModelState.AddModelError("FullName", "Required");
            var employee = new Employee { FullName = "" };

            // Act
            var result = await controller.Create(employee, "email", "pass", false);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(employee, viewResult.Model);
        }

        [Theory]
        [InlineData("test@test.com", "test@test.com")]
        [InlineData("user", "user@stockflow.pro")]
        [InlineData(null, "")]
        public void NormalizeEmail_Works(string email, string expected)
        {
            var controller = new AdminEmployeesController(null, null);
            var method = typeof(AdminEmployeesController).GetMethod("NormalizeEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var result = (string)method.Invoke(null, new object[] { email });
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task Create_SetsCorrectRole_BasedOnPosition()
        {
            // Arrange
            var context = GetDatabaseContext();
            var userManagerMock = GetMockUserManager();
            userManagerMock.Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new AdminEmployeesController(context, userManagerMock.Object);
            
            // Scanner
            var e1 = new Employee { FullName = "S1", Position = "Scanner", Phone = "1" };
            await controller.Create(e1, "s1", "pass", true);
            userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), "Scanner"), Times.Once);

            // Packer
            var e2 = new Employee { FullName = "P1", Position = "Packer", Phone = "2" };
            await controller.Create(e2, "p1", "pass", true);
            userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), "Packer"), Times.Once);

            // Office
            var e3 = new Employee { FullName = "O1", Position = "Office", Phone = "3" };
            await controller.Create(e3, "o1", "pass", true);
            userManagerMock.Verify(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), "OfficeWorker"), Times.Once);
        }
    }
}
