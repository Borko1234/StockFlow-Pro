using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.ViewModels.Admin;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly FoodieDbContext _context;

        public AdminOrdersController(FoodieDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, OrderStatus? status, int? facilityId)
        {
            var query = _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.OrderProcessing)
                .ThenInclude(op => op.PreparedByEmployee)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1));

            if (status.HasValue)
                query = query.Where(o => o.OrderStatus == status.Value);

            if (facilityId.HasValue)
                query = query.Where(o => o.FacilityId == facilityId.Value);

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Facilities = new SelectList(await _context.Facilities.ToListAsync(), "Id", "Name", facilityId);
            ViewBag.Statuses = new SelectList(Enum.GetValues(typeof(OrderStatus)));

            var model = new AdminOrderListViewModel
            {
                Orders = orders,
                StartDate = startDate,
                EndDate = endDate,
                Status = status,
                FacilityId = facilityId
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderProcessing)
                .ThenInclude(op => op.CreatedByEmployee)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            // Allow marking as Delivered from Scanned (or even Pending/Created if admin forces it)
            if (order.OrderStatus != OrderStatus.Delivered)
            {
                order.OrderStatus = OrderStatus.Delivered;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
