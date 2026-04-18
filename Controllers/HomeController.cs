using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.ViewModels;

namespace StockFlowPro.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(FoodieDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Scanner"))
            {
                return RedirectToAction("Index", "Scanner");
            }
            if (User.IsInRole("Packer"))
            {
                return RedirectToAction("Index", "Packer");
            }

            var user = await _userManager.GetUserAsync(User);
            var userName = user?.UserName ?? User.Identity.Name;

            // KPI Calculations
            var totalProducts = await _context.Products.CountAsync();
            var lowStockCount = await _context.Products.CountAsync(p => p.QuantityInStock < 10);
            var facilityCount = await _context.Facilities.CountAsync(f => f.IsActive);
            var activeOrdersCount = await _context.Orders.CountAsync(o => o.OrderStatus != Models.Enums.OrderStatus.Delivered);

            // Recent Orders
            var recentOrders = await _context.Orders
                .Include(o => o.Facility)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Weekly Movement Data for Chart.js
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var movements = await _context.Orders
                .Where(o => o.CreatedAt >= last7Days.First())
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var weeklyMovements = last7Days.Select(d => new MonthlyMovementDto
            {
                Day = d.ToString("ddd"),
                OrdersCount = movements.FirstOrDefault(m => m.Date == d)?.Count ?? 0
            }).ToList();

            var model = new DashboardViewModel
            {
                UserName = userName,
                TotalProducts = totalProducts,
                LowStockCount = lowStockCount,
                FacilityCount = facilityCount,
                ActiveOrdersCount = activeOrdersCount,
                RecentOrders = recentOrders,
                WeeklyMovements = weeklyMovements,
                // Ensure office workers only see today's orders in the recent list if necessary
                // but following the Dashboard requirement for overall KPIs
            };

            return View(model);
        }
    }
}
