using StockFlowPro.Models;
using StockFlowPro.ViewModels;
using StockFlowPro.ViewModels.Admin;
using System.Collections.Generic;
using Xunit;

namespace StockFlowPro.Tests.ViewModels
{
    public class ViewModelsTests
    {
        [Fact]
        public void AdminDashboardViewModel_Properties_Work()
        {
            var model = new AdminDashboardViewModel
            {
                TotalOrders = 10,
                PendingOrders = 5,
                TotalRevenue = 1000,
                LowStockProducts = new List<Product>(),
                TopProducts = new List<ProductPerformanceDto>(),
                MonthlyProfits = new List<MonthlyProfitDto>()
            };

            Assert.Equal(10, model.TotalOrders);
            Assert.Equal(5, model.PendingOrders);
            Assert.Equal(1000, model.TotalRevenue);
        }

        [Fact]
        public void LoginViewModel_Properties_Work()
        {
            var model = new LoginViewModel
            {
                Email = "test@test.com",
                Password = "pass",
                RememberMe = true
            };

            Assert.Equal("test@test.com", model.Email);
            Assert.Equal("pass", model.Password);
            Assert.True(model.RememberMe);
        }

        [Fact]
        public void ScannerViewModel_Properties_Work()
        {
            var model = new ScannerViewModel
            {
                CurrentOrder = new Order { Id = 1 },
                Packers = new List<ScannerPackerViewModel>(),
                ErrorMessage = "Error",
                SuccessMessage = "Success"
            };

            Assert.Equal(1, model.CurrentOrder.Id);
            Assert.Equal("Error", model.ErrorMessage);
            Assert.Equal("Success", model.SuccessMessage);
        }
    }
}
