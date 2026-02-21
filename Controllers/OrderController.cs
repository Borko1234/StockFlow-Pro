using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
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

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            var products = await _context.Products.ToListAsync();
            var productOptions = products
                .Select(p => new
                {
                    p.Id,
                    DisplayName = $"{p.Id} - {p.Name} ({p.Brand})"
                })
                .ToList();

            var viewModel = new CreateOrderViewModel
            {
                Facilities = new SelectList(facilities, "Id", "Name"),
                Products = new SelectList(productOptions, "Id", "DisplayName")
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Filter out empty items
                var validItems = model.OrderItems
                    .Where(i => i.Quantity > 0 && i.ProductId > 0)
                    .Select(i => new OrderItemDto 
                    { 
                        ProductId = i.ProductId, 
                        Quantity = i.Quantity 
                    }).ToList();

                if (!validItems.Any())
                {
                    ModelState.AddModelError("", "Please add at least one product.");
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

            // Reload lists if failed
            var facilities = await _context.Facilities.Where(f => f.IsActive).ToListAsync();
            var products = await _context.Products.ToListAsync();
            var productOptions = products
                .Select(p => new
                {
                    p.Id,
                    DisplayName = $"{p.Id} - {p.Name} ({p.Brand})"
                })
                .ToList();
            model.Facilities = new SelectList(facilities, "Id", "Name");
            model.Products = new SelectList(productOptions, "Id", "DisplayName");

            return View(model);
        }
    }
}
