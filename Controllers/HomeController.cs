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
            if (User.IsInRole("Driver"))
            {
                return RedirectToAction("Index", "Driver");
            }

            var user = await _userManager.GetUserAsync(User);
            var userName = user?.UserName ?? User.Identity.Name;

            IQueryable<Order> ordersQuery = _context.Orders.Include(o => o.Facility);

            // Office Workers see today's orders
            if (User.IsInRole("OfficeWorker"))
            {
                var today = DateTime.Today;
                ordersQuery = ordersQuery.Where(o => o.CreatedAt.Date == today);
            }
            // Admins see all orders

            var orders = await ordersQuery.OrderByDescending(o => o.CreatedAt).ToListAsync();

            var model = new HomeViewModel
            {
                UserName = userName,
                Orders = orders
            };

            return View(model);
        }
    }
}
