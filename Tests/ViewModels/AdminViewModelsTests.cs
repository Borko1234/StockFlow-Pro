using StockFlowPro.ViewModels.Admin;
using System.Collections.Generic;
using Xunit;

namespace StockFlowPro.Tests.ViewModels
{
    public class AdminViewModelsTests
    {
        [Fact]
        public void UserRoleViewModel_Properties_Work()
        {
            var model = new UserRoleViewModel
            {
                UserId = "1",
                Email = "test@test.com",
                UserName = "test",
                Roles = new List<string> { "Admin" }
            };

            Assert.Equal("1", model.UserId);
            Assert.Equal("test@test.com", model.Email);
            Assert.Equal("test", model.UserName);
            Assert.Equal("Admin", model.Roles[0]);
        }
    }
}
