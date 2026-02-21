using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.ViewModels.Admin;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly StockFlowDbContext _context;
        private readonly IOrderService _orderService;

        public AdminOrdersController(StockFlowDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, OrderStatus? status, int? facilityId)
        {
            var query = _context.Orders
                .Include(o => o.Facility)
                .Include(o => o.OrderProcessing)
                .ThenInclude(op => op.PreparedByEmployee) // For displaying who processed it
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(o => o.CreatedAt >= startDate.Value);
            
            if (endDate.HasValue)
                query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1)); // Include end date

            if (status.HasValue)
                query = query.Where(o => o.OrderStatus == status.Value);

            if (facilityId.HasValue)
                query = query.Where(o => o.FacilityId == facilityId.Value);

            var orders = await query
                .OrderBy(o => o.OrderStatus == OrderStatus.Scanned ? 0 :
                              o.OrderStatus == OrderStatus.Pending ? 1 :
                              o.OrderStatus == OrderStatus.Delivered ? 2 : 3)
                .ThenByDescending(o => o.CreatedAt)
                .ToListAsync();

            ViewBag.Facilities = new SelectList(await _context.Facilities.ToListAsync(), "Id", "Name", facilityId);
            ViewBag.Statuses = new SelectList(GetDisplayStatuses());

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
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderProcessing)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();

            if (order.OrderStatus == newStatus)
            {
                return Json(new { success = false, message = "Status is already set." });
            }

            if (newStatus == OrderStatus.Pending && order.OrderProcessing != null)
            {
                order.OrderProcessing.PreparedByEmployeeId = null;
                order.OrderProcessing.PreparedByEmployee = null;
            }

            var oldStatus = order.OrderStatus;
            var updated = await _orderService.UpdateOrderStatusAsync(id, newStatus);
            if (!updated)
            {
                return Json(new { success = false, message = "Unable to update order status." });
            }

            var audit = new OrderStatusAuditLog
            {
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                AdminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown",
                AdminUserName = User.Identity?.Name ?? "Unknown"
            };

            _context.OrderStatusAuditLogs.Add(audit);
            await _context.SaveChangesAsync();

            return Json(new { success = true, status = newStatus.ToString() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkDelivered(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (order.OrderStatus != OrderStatus.Scanned)
            {
                return RedirectToAction(nameof(Index));
            }

            var oldStatus = order.OrderStatus;
            var updated = await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Delivered);
            if (!updated)
            {
                return RedirectToAction(nameof(Index));
            }

            var audit = new OrderStatusAuditLog
            {
                OrderId = order.Id,
                OldStatus = oldStatus,
                NewStatus = OrderStatus.Delivered,
                AdminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown",
                AdminUserName = User.Identity?.Name ?? "Unknown"
            };

            _context.OrderStatusAuditLogs.Add(audit);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private static OrderStatus[] GetDisplayStatuses()
        {
            return new[]
            {
                OrderStatus.Scanned,
                OrderStatus.Pending,
                OrderStatus.Delivered,
                OrderStatus.Cancelled
            };
        }

        private static int GetWorkflowIndex(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => 0,
                OrderStatus.Scanned => 1,
                OrderStatus.Delivered => 2,
                OrderStatus.Cancelled => 3,
                _ => int.MaxValue
            };
        }
    }
}
