using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Controllers;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.ViewModels.Admin;
using Xunit;

namespace StockFlowPro.Tests.Controllers
{
    public class AdminPayrollControllerTests
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
        public async Task Index_ReturnsView_WithPayrollData()
        {
            // Arrange
            var context = GetDatabaseContext();
            var employee = new Employee { FullName = "Emp 1", Position = "Packer", Phone = "1" };
            context.Employees.Add(employee);
            context.OrderProcessings.Add(new OrderProcessing { PreparedByEmployeeId = employee.Id, ProcessDate = DateTime.Today, Order = new Order { Id = 1, OrderStatus = StockFlowPro.Models.Enums.OrderStatus.Prepared, FacilityId = 1 } });
            await context.SaveChangesAsync();

            var controller = new AdminPayrollController(context);

            // Act
            var result = await controller.Index(null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PayrollViewModel>(viewResult.ViewData.Model);
            Assert.Single(model.Items);
            Assert.Equal(1, model.Items.First().OrdersCount);
        }
    }
}
