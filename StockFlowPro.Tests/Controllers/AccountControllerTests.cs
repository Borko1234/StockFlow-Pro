using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StockFlowPro.Controllers;
using StockFlowPro.ViewModels;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AccountControllerTests
    {
        private static (Mock<UserManager<IdentityUser>>, Mock<SignInManager<IdentityUser>>) CreateManagers()
        {
            var store = new Mock<IUserStore<IdentityUser>>();
            var userManager = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            var signInManager = new Mock<SignInManager<IdentityUser>>(userManager.Object, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
            return (userManager, signInManager);
        }

        [Fact]
        public void Login_Get_Returns_View()
        {
            var (userMgr, signInMgr) = CreateManagers();
            var controller = new AccountController(signInMgr.Object, userMgr.Object);
            var result = controller.Login();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_Post_Invalid_Model_Returns_View()
        {
            var (userMgr, signInMgr) = CreateManagers();
            var controller = new AccountController(signInMgr.Object, userMgr.Object);
            controller.ModelState.AddModelError("Email", "Required");
            var result = await controller.Login(new LoginViewModel());
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Login_Post_Admin_Redirects_To_Admin_Index()
        {
            var (userMgr, signInMgr) = CreateManagers();
            signInMgr.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            var user = new IdentityUser { Email = "admin@test.com", UserName = "admin@test.com" };
            userMgr.Setup(u => u.FindByEmailAsync("admin@test.com")).ReturnsAsync(user);
            userMgr.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);

            var controller = new AccountController(signInMgr.Object, userMgr.Object);
            var result = await controller.Login(new LoginViewModel { Email = "admin@test.com", Password = "x", RememberMe = false });
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Login_Post_NonAdmin_Redirects_To_Home_Index()
        {
            var (userMgr, signInMgr) = CreateManagers();
            signInMgr.Setup(s => s.PasswordSignInAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            var user = new IdentityUser { Email = "user@test.com", UserName = "user@test.com" };
            userMgr.Setup(u => u.FindByEmailAsync("user@test.com")).ReturnsAsync(user);
            userMgr.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

            var controller = new AccountController(signInMgr.Object, userMgr.Object);
            var result = await controller.Login(new LoginViewModel { Email = "user@test.com", Password = "x", RememberMe = false });
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }

        [Fact]
        public async Task Logout_Redirects_To_Login()
        {
            var (userMgr, signInMgr) = CreateManagers();
            var controller = new AccountController(signInMgr.Object, userMgr.Object);
            var result = await controller.Logout();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }
    }
}
