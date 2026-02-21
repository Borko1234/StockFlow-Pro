using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Packer,Admin")]
    public class PackerController : Controller
    {
        private readonly StockFlowDbContext _context;

        public PackerController(StockFlowDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var orders = await _context.Orders
                .Include(o => o.Facility)
                .Where(o => o.OrderStatus == OrderStatus.Pending)
                .Where(o => o.CreatedAt >= today && o.CreatedAt < tomorrow)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            ViewData["DashboardTitle"] = "Packer Dashboard";
            ViewData["DashboardSubtitle"] = "Pending orders ready for packing";

            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> MarkPacked(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null && order.OrderStatus == OrderStatus.Scanned)
            {
                // Assuming next step is Scanned or Delivered, but Packer usually prepares for Driver.
                // For now let's assume Packer makes it ready for delivery or just acknowledges.
                // The prompt didn't specify Packer state transition, but let's assume they move it to 'Scanned' or keep it 'Prepared' but verified.
                // Wait, the prompt says "Packer Role: Design a simple home page view showing orders".
                // Let's just keep it simple view.
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
