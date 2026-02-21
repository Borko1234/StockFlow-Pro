using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.ViewModels;

namespace StockFlowPro.Controllers
{
    [Authorize]
    [Authorize(Roles = "Scanner,Admin")]
    public class ScannerController : Controller
    {
        private readonly StockFlowDbContext _context;
        private readonly IOrderService _orderService;

        public ScannerController(StockFlowDbContext context, IOrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        // GET: Scanner
        public async Task<IActionResult> Index(int? orderId)
        {
            var model = new ScannerViewModel
            {
                Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync(),
                OrderId = orderId
            };

            if (orderId.HasValue)
            {
                var order = await _context.Orders
                    .Include(o => o.Facility)
                    .Include(o => o.OrderItems)
                    .Include(o => o.OrderProcessing)
                    .FirstOrDefaultAsync(o => o.Id == orderId.Value);

                if (order == null)
                {
                    model.ErrorMessage = "Order not found.";
                }
                else if (order.OrderStatus != OrderStatus.Pending)
                {
                    model.ErrorMessage = "Only Pending orders can be scanned.";
                }
                else
                {
                    model.CurrentOrder = order;
                    if (order.OrderProcessing?.PreparedByEmployeeId.HasValue == true)
                    {
                        model.SelectedEmployeeId = order.OrderProcessing.PreparedByEmployeeId;
                    }
                }
            }

            return View(model);
        }

        // POST: Scanner/Assign
        [HttpPost]
        public async Task<IActionResult> Assign(ScannerViewModel model)
        {
            if (model.OrderId.HasValue && model.SelectedEmployeeId.HasValue)
            {
                var order = await _context.Orders
                    .Include(o => o.OrderProcessing)
                    .FirstOrDefaultAsync(o => o.Id == model.OrderId.Value);

                if (order != null)
                {
                    if (order.OrderStatus != OrderStatus.Pending)
                    {
                        model.ErrorMessage = "Only Pending orders can be scanned.";
                        model.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
                        return View("Index", model);
                    }

                    if (order.OrderProcessing == null)
                    {
                        order.OrderProcessing = new OrderProcessing { OrderId = order.Id };
                        _context.OrderProcessings.Add(order.OrderProcessing);
                    }

                    order.OrderProcessing.PreparedByEmployeeId = model.SelectedEmployeeId.Value;
                    
                    await _context.SaveChangesAsync();

                    return RedirectToAction("Products", new { orderId = order.Id });
                }
            }

            // Reload employees if redisplaying view
            model.Employees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
            return View("Index", model);
        }

        // GET: Scanner/Products
        public async Task<IActionResult> Products(int orderId, string searchText)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return RedirectToAction("Index");
            }

            var model = new ProductsViewModel
            {
                OrderId = orderId,
                CurrentOrder = order,
                SearchText = searchText
            };

            return View(model);
        }

        // POST: Scanner/ScanItem
        [HttpPost]
        public async Task<IActionResult> ScanItem(int orderId, string barcode)
        {
            if (!int.TryParse(barcode, out var productId))
            {
                return Json(new { success = false, message = "Enter a valid numeric product ID." });
            }

            var item = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == orderId && oi.Product.Id == productId)
                .FirstOrDefaultAsync();

            if (item != null)
            {
                return Json(new
                {
                    success = true,
                    productId = item.Product.Id,
                    productName = item.Product.Name,
                    displayName = $"{item.Product.Name} ({item.Product.Id})",
                    quantity = item.Quantity
                });
            }

            return Json(new { success = false, message = "Product ID not found in this order." });
        }

        // POST: Scanner/CompleteScan
        [HttpPost]
        public async Task<IActionResult> CompleteScan(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." });
            }

            if (order.OrderStatus != OrderStatus.Pending)
            {
                return Json(new { success = false, message = "Order is not in Pending status." });
            }

            var updated = await _orderService.UpdateOrderStatusAsync(orderId, OrderStatus.Scanned);
            return Json(new { success = updated });
        }
    }
}
