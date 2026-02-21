using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockFlowPro.Data;
using StockFlowPro.Services;
using StockFlowPro.ViewModels.Order;

namespace StockFlowPro.Controllers
{
    [Authorize(Roles = "OfficeWorker,Admin")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly StockFlowDbContext _context;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, StockFlowDbContext context, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Create));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CreateOrderViewModel
            {
                OrderItems = new List<OrderItemViewModel> { new OrderItemViewModel() }
            };

            await PopulateListsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            await PopulateListsAsync(model);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                _logger.LogWarning("Order creation model validation failed: {Errors}", string.Join("; ", errors));
                return View(model);
            }

            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == model.FacilityId && f.IsActive);
            if (facility == null)
            {
                ModelState.AddModelError(nameof(model.FacilityId), "Selected facility not found.");
                _logger.LogWarning("Order creation failed: facility not found {FacilityId}", model.FacilityId);
                return View(model);
            }

            var inputItems = model.OrderItems ?? new List<OrderItemViewModel>();
            var filteredItems = inputItems
                .Where(i => i.ProductId > 0 && i.Quantity > 0)
                .ToList();

            if (!filteredItems.Any())
            {
                ModelState.AddModelError(string.Empty, "Please add at least one product.");
                _logger.LogWarning("Order creation failed: no valid items for facility {FacilityId}", model.FacilityId);
                return View(model);
            }

            var distinctProductIds = filteredItems
                .Select(i => i.ProductId)
                .Distinct()
                .ToList();

            var existingProductIds = await _context.Products
                .Where(p => distinctProductIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var missingProductIds = distinctProductIds
                .Except(existingProductIds)
                .ToList();

            if (missingProductIds.Any())
            {
                ModelState.AddModelError(string.Empty, $"Invalid product selection: {string.Join(", ", missingProductIds)}");
                _logger.LogWarning("Order creation failed: missing products {ProductIds}", string.Join(", ", missingProductIds));
                return View(model);
            }

            var items = filteredItems
                .GroupBy(i => i.ProductId)
                .Select(g => new OrderItemDto
                {
                    ProductId = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            try
            {
                await _orderService.CreateOrderAsync(model.FacilityId, items);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order creation failed for facility {FacilityId} with {ItemCount} items", model.FacilityId, items.Count);
                ModelState.AddModelError(string.Empty, "Error creating order. Check server logs for details.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePresetOrder()
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.IsActive && f.Name == "Burgas DC Izgrev");
            if (facility == null)
            {
                _logger.LogWarning("Preset order failed: facility not found");
                var missingFacilityModel = new CreateOrderViewModel();
                ModelState.AddModelError(string.Empty, "Preset facility not found.");
                await PopulateListsAsync(missingFacilityModel);
                return View("Create", missingFacilityModel);
            }

            var product = await _context.Products.FindAsync(67);
            if (product == null)
            {
                _logger.LogWarning("Preset order failed: product not found");
                var missingProductModel = new CreateOrderViewModel();
                ModelState.AddModelError(string.Empty, "Preset product not found.");
                await PopulateListsAsync(missingProductModel);
                return View("Create", missingProductModel);
            }

            try
            {
                await _orderService.CreateOrderAsync(facility.Id, new List<OrderItemDto>
                {
                    new OrderItemDto { ProductId = product.Id, Quantity = 13 }
                });
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Preset order creation failed for facility {FacilityId} and product {ProductId}", facility.Id, product.Id);
                var errorModel = new CreateOrderViewModel();
                ModelState.AddModelError(string.Empty, "Error creating preset order. Check server logs for details.");
                await PopulateListsAsync(errorModel);
                return View("Create", errorModel);
            }
        }

        private async Task PopulateListsAsync(CreateOrderViewModel model)
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
            model.Facilities = new SelectList(facilities, "Id", "Name");
            model.Products = new SelectList(productOptions, "Id", "DisplayName");
            if (model.OrderItems == null || model.OrderItems.Count == 0)
            {
                model.OrderItems = new List<OrderItemViewModel> { new OrderItemViewModel() };
            }
        }
    }
}
