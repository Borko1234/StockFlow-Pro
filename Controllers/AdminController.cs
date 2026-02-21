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
            var pendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Created); // Or Pending if enum changed back
            
            // REVENUE FIX: Only include DELIVERED orders
            var totalRevenue = await _context.Orders
                .Where(o => o.OrderStatus == OrderStatus.Delivered)
                .SumAsync(o => o.TotalAmount);

            // Top 3 Products (Calculated based on Delivered orders)
            var topProductsQuery = await _context.OrderItems
                .Where(oi => oi.Order.OrderStatus == OrderStatus.Delivered)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new 
                {
                    ProductId = g.Key,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(3)
                .ToListAsync();

            var productIds = topProductsQuery.Select(x => x.ProductId).ToList();
            var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
            
            var totalSoldUnits = await _context.OrderItems
                .Where(oi => oi.Order.OrderStatus == OrderStatus.Delivered)
                .SumAsync(oi => oi.Quantity);

            var topProducts = topProductsQuery.Select(x => 
            {
                var p = products.FirstOrDefault(prod => prod.Id == x.ProductId);
                var cost = p?.CostPrice ?? 0;
                var profit = x.Revenue - (x.UnitsSold * cost); 
                return new ProductPerformanceDto
                {
                    ProductName = p?.Name ?? "Unknown",
                    UnitsSold = x.UnitsSold,
                    Revenue = x.Revenue,
                    ProfitMargin = x.Revenue > 0 ? (profit / x.Revenue) * 100 : 0,
                    PercentageOfTotal = totalSoldUnits > 0 ? ((double)x.UnitsSold / totalSoldUnits) * 100 : 0
                };
            }).ToList();

            // Monthly Profits (Last 6 months) - Delivered Only
            var sixMonthsAgo = DateTime.Today.AddMonths(-6);
            var monthlyData = await _context.Orders
                .Where(o => o.CreatedAt >= sixMonthsAgo && o.OrderStatus == OrderStatus.Delivered)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .ToListAsync();

            var monthlyProfits = monthlyData
                .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                .Select(g => new MonthlyProfitDto
                {
                    Month = $"{g.Key.Month}/{g.Key.Year}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    Profit = g.Sum(o => o.OrderItems.Sum(oi => oi.TotalPrice - (oi.Quantity * oi.Product.CostPrice)))
                })
                .OrderBy(m => new DateTime(int.Parse(m.Month.Split('/')[1]), int.Parse(m.Month.Split('/')[0]), 1))
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
