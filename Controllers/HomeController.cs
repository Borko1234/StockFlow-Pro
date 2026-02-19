using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
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
            var user = await _userManager.GetUserAsync(User);
            var userName = user?.UserName ?? User.Identity.Name;

            var today = DateTime.Today;
            var orders = await _context.Orders
                .Include(o => o.Facility)
                .Where(o => o.CreatedAt.Date == today)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var model = new HomeViewModel
            {
                UserName = userName,
                TodaysOrders = orders
            };

            return View(model);
        }
    }
}
