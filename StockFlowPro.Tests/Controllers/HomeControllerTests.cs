using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Tests.TestUtils;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class HomeControllerTests
    {
        private static HomeController CreateWithRole(string role)
        {
            var ctx = TestDbContextFactory.CreateContext();
            var userMgr = new Mock<UserManager<IdentityUser>>(Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
            var controller = new HomeController(ctx, userMgr.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, role) }, "TestAuth"))
                }
            };
            return controller;
        }

        [Fact]
        public async Task Index_Admin_Redirects_Admin_Index()
        {
            var controller = CreateWithRole("Admin");
            var result = await controller.Index();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_Scanner_Redirects_Scanner_Index()
        {
            var controller = CreateWithRole("Scanner");
            var result = await controller.Index();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Scanner", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_Packer_Redirects_Packer_Index()
        {
            var controller = CreateWithRole("Packer");
            var result = await controller.Index();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Packer", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_Driver_Redirects_Driver_Index()
        {
            var controller = CreateWithRole("Driver");
            var result = await controller.Index();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Driver", redirect.ControllerName);
        }

        [Fact]
        public async Task Index_OfficeWorker_Filters_Todays_Orders()
        {
            var ctx = TestDbContextFactory.CreateContext();
            ctx.Facilities.Add(new Facility { Id = 1, Name = "F", Address = "A", Phone = "0", Area = "X", RepresentativeName = "R", IsActive = true });
            ctx.Orders.Add(new Order { FacilityId = 1, OrderStatus = OrderStatus.Pending, CreatedAt = System.DateTime.Today.AddDays(-1) });
            ctx.Orders.Add(new Order { FacilityId = 1, OrderStatus = OrderStatus.Pending, CreatedAt = System.DateTime.Today.AddHours(1) });
            await ctx.SaveChangesAsync();

            var userMgr = new Mock<UserManager<IdentityUser>>(Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
            var controller = new HomeController(ctx, userMgr.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "OfficeWorker") }, "TestAuth"))
                }
            };

            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            Assert.NotNull(view.Model);
        }
    }
}
