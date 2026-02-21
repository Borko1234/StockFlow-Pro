using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Models;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminEmployeesControllerTests
    {
        private static Mock<UserManager<IdentityUser>> CreateUserManager()
        {
            return new Mock<UserManager<IdentityUser>>(Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Create_Invalid_Model_Returns_View()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var userMgr = CreateUserManager();
            var controller = new AdminEmployeesController(ctx, userMgr.Object, NullLogger<AdminEmployeesController>.Instance);
            controller.ModelState.AddModelError("Email", "Required");

            var result = await controller.Create(new Employee(), "", "");
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Success_Redirects_Index()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var userMgr = CreateUserManager();
            userMgr.Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userMgr.Setup(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            var controller = new AdminEmployeesController(ctx, userMgr.Object, NullLogger<AdminEmployeesController>.Instance);
            var emp = new Employee { FullName = "Test", Position = "Scanner", Phone = "000", IsActive = true };
            var result = await controller.Create(emp, "user@test.com", "Pass123!");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.True(await ctx.Employees.AnyAsync());
        }

        [Fact]
        public async Task Create_Identity_Failure_Returns_View_With_ModelErrors()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var userMgr = CreateUserManager();
            userMgr.Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()))
                   .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "E", Description = "Err" }));
            var controller = new AdminEmployeesController(ctx, userMgr.Object, NullLogger<AdminEmployeesController>.Instance);
            var emp = new Employee { FullName = "Test", Position = "Scanner", Phone = "000", IsActive = true };
            var result = await controller.Create(emp, "user@test.com", "Pass123!");
            var view = Assert.IsType<ViewResult>(result);
            Assert.False(controller.ModelState.IsValid);
        }

        [Fact]
        public async Task Edit_Updates_Employee_And_Redirects()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var userMgr = CreateUserManager();
            var controller = new AdminEmployeesController(ctx, userMgr.Object, NullLogger<AdminEmployeesController>.Instance);
            var emp = new Employee { FullName = "Test", Position = "Scanner", Phone = "000", IsActive = true, UserId = "uid" };
            ctx.Employees.Add(emp);
            await ctx.SaveChangesAsync();

            emp.FullName = "Updated";
            var result = await controller.Edit(emp.Id, emp);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_Removes_Employee_When_User_Missing()
        {
            using var ctx = TestDbContextFactory.CreateContext();
            var userMgr = CreateUserManager();
            var controller = new AdminEmployeesController(ctx, userMgr.Object, NullLogger<AdminEmployeesController>.Instance);
            var emp = new Employee { FullName = "ToDel", Position = "Scanner", Phone = "000", IsActive = true, UserId = "missing" };
            ctx.Employees.Add(emp);
            await ctx.SaveChangesAsync();

            var result = await controller.DeleteConfirmed(emp.Id);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.False(await ctx.Employees.AnyAsync());
        }
    }
}
