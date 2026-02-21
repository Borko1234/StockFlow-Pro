using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Services;

namespace StockFlowPro.Tests.TestUtils
{
    public static class TestDbContextFactory
    {
        public static StockFlowDbContext CreateContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<StockFlowDbContext>()
                .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;
            return new StockFlowDbContext(options);
        }
    }

    public static class Builders
    {
        public static Product BuildProduct(int id = 1, string name = "Test Product", string brand = "Brand", decimal price = 10m, int qty = 100)
        {
            return new Product
            {
                Id = id,
                Name = name,
                Brand = brand,
                Price = price,
                CostPrice = price / 2,
                QuantityInStock = qty,
                MinimumStockLevel = 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static Facility BuildFacility(int id = 1, string name = "Facility A", bool isActive = true)
        {
            return new Facility
            {
                Id = id,
                Name = name,
                Address = "Addr",
                Phone = "000",
                Area = "Area",
                RepresentativeName = "Rep",
                IsActive = isActive,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
