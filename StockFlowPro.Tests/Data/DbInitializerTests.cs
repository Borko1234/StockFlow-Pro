using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StockFlowPro.Data;
using Xunit;

namespace StockFlowPro.Tests.Data
{
    public class DbInitializerTests
    {
        [Fact]
        public async Task Initialize_Creates_Roles_And_Admin_User()
        {
            var services = new ServiceCollection();
            services.AddDbContext<StockFlowPro.Data.StockFlowDbContext>(o => { o.UseApplicationServiceProvider(services.BuildServiceProvider()); });

            var roleManager = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(), null, null, null, null);
            roleManager.Setup(r => r.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            roleManager.Setup(r => r.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);

            var userManager = new Mock<UserManager<IdentityUser>>(
                Mock.Of<IUserStore<IdentityUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(u => u.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((IdentityUser)null);
            userManager.Setup(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(u => u.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            services.AddSingleton(userManager.Object);
            services.AddSingleton(roleManager.Object);

            var sp = services.BuildServiceProvider();
            await DbInitializer.Initialize(sp);

            roleManager.Verify(r => r.CreateAsync(It.IsAny<IdentityRole>()), Times.AtLeastOnce());
            userManager.Verify(u => u.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()), Times.AtLeastOnce());
        }
    }
}
