using System;
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
    [Authorize(Roles = "Scanner,Admin")]
    public class ScannerController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly FoodieDbContext _context;

        public ScannerController(IOrderService orderService, FoodieDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new ScannerViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Scan(int orderId)
        {
            var model = new ScannerViewModel();
            
            try
            {
                var result = await _orderService.ScanOrderAsync(orderId);
                if (result)
                {
                    model.IsSuccess = true;
                    model.SuccessMessage = $"Order #{orderId} scanned successfully and moved to Prepared.";
                    
                    // Fetch order for display
                    model.CurrentOrder = await _context.Orders
                        .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                        .FirstOrDefaultAsync(o => o.Id == orderId);
                }
                else
                {
                    model.IsSuccess = false;
                    model.ErrorMessage = $"Order #{orderId} not found or not in correct status.";
                }
            }
            catch (Exception ex)
            {
                model.IsSuccess = false;
                model.ErrorMessage = $"Error scanning order: {ex.Message}";
            }

            return View("Index", model);
        }
    }
}
