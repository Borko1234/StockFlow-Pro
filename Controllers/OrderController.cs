using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;
using StockFlowPro.Services;
using StockFlowPro.ViewModels.Order;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "OfficeWorker,Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly FoodieDbContext _context;

        public OrderController(IOrderService orderService, FoodieDbContext context)
        {
            _orderService = orderService;
            _context = context;
        }

        public async Task<IActionResult> Create()
        {
            var model = new CreateOrderViewModel
            {
                OrderItems = new List<OrderItemViewModel> { new OrderItemViewModel() }
            };
            await PopulateLists(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                var validItems = model.OrderItems
                    .Where(i => i.ProductId > 0 && i.Quantity > 0)
                    .Select(i => new OrderItemDto { ProductId = i.ProductId, Quantity = i.Quantity })
                    .ToList();

                if (!validItems.Any())
                {
                    ModelState.AddModelError("", "Please add at least one product.");
                }
                else
                {
                    // Check Facility
                    var facility = await _context.Facilities.FindAsync(model.FacilityId);
                    if (facility == null)
                    {
                        ModelState.AddModelError("FacilityId", "Invalid Facility.");
                    }
                    else
                    {
                        try
                        {
                            await _orderService.CreateOrderAsync(model.FacilityId, validItems);
                            return RedirectToAction("Index", "Home");
                        }
                        catch (Exception ex)
                        {
                            ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                        }
                    }
                }
            }
            
            await PopulateLists(model);
            return View(model);
        }

        private async Task PopulateLists(CreateOrderViewModel model)
        {
            model.Facilities = new SelectList(await _context.Facilities.Where(f => f.IsActive).ToListAsync(), "Id", "Name");
            var products = await _context.Products.Select(p => new { p.Id, DisplayName = $"{p.Id} - {p.Name}" }).ToListAsync();
            model.Products = new SelectList(products, "Id", "DisplayName");
        }
    }
}
