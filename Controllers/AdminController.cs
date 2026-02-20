using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models.Enums;
using StockFlowPro.ViewModels.Admin;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly FoodieDbContext _context;

        public AdminController(FoodieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Analytics Logic
            var totalOrders = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending);
            var totalRevenue = await _context.Orders.SumAsync(o => o.TotalAmount);

            // Top 3 Products
            // Group by ProductId in OrderItems
            var topProductsQuery = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new 
                {
                    ProductId = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    // Cost is harder to get in GroupBy if it varies, but assuming Product.CostPrice is constant for simplicity or we need to join.
                    // EF Core 9 translation for this might be complex. Let's fetch data and process in memory if dataset is small, 
                    // or use a more optimized query. Given constraints, let's try projection.
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(3)
                .ToListAsync();

            var productIds = topProductsQuery.Select(x => x.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();

            var topProducts = topProductsQuery.Select(x => 
            {
                var p = products.FirstOrDefault(prod => prod.Id == x.ProductId);
                var cost = p?.CostPrice ?? 0;
                var profit = x.Revenue - (x.UnitsSold * cost); // Approx profit
                return new ProductPerformanceDto
                {
                    ProductName = p?.Name ?? "Unknown",
                    UnitsSold = x.UnitsSold,
                    Revenue = x.Revenue,
                    ProfitMargin = x.Revenue > 0 ? (profit / x.Revenue) * 100 : 0,
                    PercentageOfTotal = totalOrders > 0 ? ((double)x.UnitsSold / _context.OrderItems.Sum(oi => oi.Quantity)) * 100 : 0 // % of total units
                };
            }).ToList();

            // Monthly Profits (Last 6 months)
            var sixMonthsAgo = DateTime.Today.AddMonths(-6);
            var monthlyData = await _context.Orders
                .Where(o => o.CreatedAt >= sixMonthsAgo)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ToListAsync(); // Fetch to memory to group by month (SQLite limitation on date functions sometimes)

            var monthlyProfits = monthlyData
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlyProfitDto
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    Profit = g.Sum(o => o.OrderItems.Sum(oi => oi.TotalPrice - (oi.Quantity * oi.Product.CostPrice)))
                })
                .OrderBy(m => m.Month) // Rough sort
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                TopProducts = topProducts,
                MonthlyProfits = monthlyProfits
            };

            return View(model);
        }
    }
}
