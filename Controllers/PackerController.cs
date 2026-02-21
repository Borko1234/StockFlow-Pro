using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models.Enums;
using StockFlowPro.Models;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Packer,Admin")]
    public class PackerController : Controller
    {
        private readonly FoodieDbContext _context;

        public PackerController(FoodieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.OrderProcessing)
                .ThenInclude(op => op.PreparedByEmployee)
                .Where(o => o.OrderStatus == OrderStatus.Pending)
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            var employees = await _context.Employees
                .Where(e => e.IsActive && e.Position == "Packer")
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.PackerEmployees = employees;
            ViewData["DashboardTitle"] = "Packer Dashboard";
            ViewData["DashboardSubtitle"] = "Pending orders ready for packing";

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPacker(int orderId, int? employeeId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderProcessing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (order.OrderProcessing == null)
            {
                order.OrderProcessing = new OrderProcessing { OrderId = order.Id };
                _context.OrderProcessings.Add(order.OrderProcessing);
            }

            order.OrderProcessing.PreparedByEmployeeId = employeeId;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
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
