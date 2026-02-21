using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Driver,Admin")]
    public class DriverController : Controller
    {
        private readonly StockFlowDbContext _context;

        public DriverController(StockFlowDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Facility)
                .Where(o => o.OrderStatus == OrderStatus.Scanned)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            ViewData["DashboardTitle"] = "Driver Dashboard";
            ViewData["DashboardSubtitle"] = "Scanned orders ready for delivery";

            return View("~/Views/Packer/Index.cshtml", orders);
        }
    }
}
