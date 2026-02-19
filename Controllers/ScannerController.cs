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
    public class ScannerController : Controller
    {
        private readonly FoodieDbContext _context;
        private readonly IOrderService _orderService;

        public ScannerController(FoodieDbContext context, IOrderService orderService)
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
                else
                {
                    model.CurrentOrder = order;
                    // Pre-select employee if already assigned
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
                    if (order.OrderProcessing == null)
                    {
                        order.OrderProcessing = new OrderProcessing { OrderId = order.Id };
                        _context.OrderProcessings.Add(order.OrderProcessing);
                    }

                    // Assign Employee
                    order.OrderProcessing.PreparedByEmployeeId = model.SelectedEmployeeId.Value;
                    
                    // Update Status if Created -> Prepared
                    if (order.OrderStatus == OrderStatus.Created)
                    {
                        // Using Service to handle status transition logic (stock check etc)
                        // But wait, OrderService.ScanOrderAsync handles Created -> Prepared.
                        // Let's use that if applicable, or just update manually.
                        // The prompt said: "When the scanner inputs the Order ID... automatically change its status to Prepared."
                        // So finding the order implies scanning.
                        
                        await _orderService.ScanOrderAsync(order.Id);
                    }
                    else
                    {
                         await _context.SaveChangesAsync();
                    }

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
             // This action can be called via AJAX to verify a product
             var item = await _context.OrderItems
                 .Include(oi => oi.Product)
                 .Where(oi => oi.OrderId == orderId && (oi.Product.Id.ToString() == barcode || oi.Product.Name == barcode)) // Assuming barcode matches ID or Name for now as Product has no Barcode field
                 .FirstOrDefaultAsync();

             if (item != null)
             {
                 return Json(new { success = true, productName = item.Product.Name, quantity = item.Quantity });
             }

             return Json(new { success = false, message = "Product not found in this order." });
        }
    }
}
