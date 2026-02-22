using System;
using System.Collections.Generic;
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
            var model = new ScannerViewModel();
            if (TempData["SuccessMessage"] is string successMessage)
            {
                model.SuccessMessage = successMessage;
            }
            if (TempData["ErrorMessage"] is string errorMessage)
            {
                model.ErrorMessage = errorMessage;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Scan(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .Include(o => o.OrderProcessing)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return View("Index", new ScannerViewModel { ErrorMessage = $"Order #{orderId} not found." });
                }

                if (order.OrderStatus != OrderStatus.Created)
                {
                    return View("Index", new ScannerViewModel { ErrorMessage = $"Order #{orderId} must be in Created status to scan." });
                }

                var remainingProductIds = BuildRemainingProductIds(order);
                var packers = await LoadPackersAsync();
                var model = BuildScannerModel(order, remainingProductIds, packers, null, null, null);
                return View("Index", model);
            }
            catch (Exception ex)
            {
                return View("Index", new ScannerViewModel { ErrorMessage = $"Error scanning order: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScanProduct(int orderId, int productId, string remainingProductIds, int? selectedPackerId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderProcessing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return View("Index", new ScannerViewModel { ErrorMessage = $"Order #{orderId} not found." });
            }

            if (order.OrderStatus != OrderStatus.Created)
            {
                return View("Index", new ScannerViewModel { ErrorMessage = $"Order #{orderId} must be in Created status to scan." });
            }

            var remaining = ParseRemainingProductIds(remainingProductIds);
            var packers = await LoadPackersAsync();

            if (productId <= 0)
            {
                var invalidModel = BuildScannerModel(order, remaining, packers, selectedPackerId, null, "Please scan a valid product ID.");
                return View("Index", invalidModel);
            }

            var index = remaining.IndexOf(productId);
            if (index < 0)
            {
                var invalidModel = BuildScannerModel(order, remaining, packers, selectedPackerId, null, $"Product ID {productId} is not part of the remaining items.");
                return View("Index", invalidModel);
            }

            remaining.RemoveAt(index);
            var model = BuildScannerModel(order, remaining, packers, selectedPackerId, "Product scanned.", null);
            return View("Index", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkOrderScanned(int orderId, string remainingProductIds, int? selectedPackerId)
        {
            var remaining = ParseRemainingProductIds(remainingProductIds);
            if (remaining.Count > 0)
            {
                TempData["ErrorMessage"] = "All products must be scanned before marking the order as scanned.";
                return RedirectToAction(nameof(Index));
            }

            if (!selectedPackerId.HasValue)
            {
                TempData["ErrorMessage"] = "Select a packer before marking the order as scanned.";
                return RedirectToAction(nameof(Index));
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderProcessing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = $"Order #{orderId} not found.";
                return RedirectToAction(nameof(Index));
            }

            if (order.OrderStatus != OrderStatus.Created)
            {
                TempData["ErrorMessage"] = $"Order #{orderId} must be in Created status to scan.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var item in order.OrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.QuantityInStock -= item.Quantity;
                    if (item.Product.QuantityInStock < 0) item.Product.QuantityInStock = 0;
                }
            }

            if (order.OrderProcessing == null)
            {
                order.OrderProcessing = new OrderProcessing
                {
                    OrderId = order.Id,
                    ProcessDate = DateTime.Now
                };
            }

            order.OrderProcessing.PreparedByEmployeeId = selectedPackerId.Value;
            order.OrderStatus = OrderStatus.Scanned;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{orderId} marked as scanned.";
            return RedirectToAction(nameof(Index));
        }

        private static List<int> BuildRemainingProductIds(Order order)
        {
            var remaining = new List<int>();
            foreach (var item in order.OrderItems)
            {
                for (var i = 0; i < item.Quantity; i++)
                {
                    remaining.Add(item.ProductId);
                }
            }
            return remaining;
        }

        private static List<int> ParseRemainingProductIds(string remainingProductIds)
        {
            if (string.IsNullOrWhiteSpace(remainingProductIds)) return new List<int>();
            return remainingProductIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(value => int.TryParse(value, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
        }

        private static ScannerViewModel BuildScannerModel(Order order, List<int> remainingProductIds, List<ScannerPackerViewModel> packers, int? selectedPackerId, string? successMessage, string? errorMessage)
        {
            var remainingCounts = remainingProductIds
                .GroupBy(id => id)
                .ToDictionary(g => g.Key, g => g.Count());

            var items = new List<ScannerItemViewModel>();
            foreach (var orderItem in order.OrderItems)
            {
                var totalCount = orderItem.Quantity;
                var remainingCount = remainingCounts.TryGetValue(orderItem.ProductId, out var count) ? count : 0;
                var scannedCount = totalCount - remainingCount;
                for (var i = 0; i < totalCount; i++)
                {
                    items.Add(new ScannerItemViewModel
                    {
                        ProductId = orderItem.ProductId,
                        ProductName = orderItem.Product?.Name ?? string.Empty,
                        Brand = orderItem.Product?.Brand ?? string.Empty,
                        Price = orderItem.Product?.Price ?? 0,
                        IsScanned = i < scannedCount
                    });
                }
            }

            var model = new ScannerViewModel
            {
                CurrentOrder = order,
                Items = items,
                RemainingProductIds = remainingProductIds,
                RemainingProductIdsCsv = string.Join(",", remainingProductIds),
                TotalItems = items.Count,
                RemainingItems = remainingProductIds.Count,
                AllItemsScanned = remainingProductIds.Count == 0,
                Packers = packers,
                SelectedPackerId = selectedPackerId,
                SuccessMessage = successMessage,
                ErrorMessage = errorMessage
            };

            return model;
        }

        private async Task<List<ScannerPackerViewModel>> LoadPackersAsync()
        {
            return await _context.Employees
                .Where(e => e.IsActive && e.Position == "Packer")
                .OrderBy(e => e.FullName)
                .Select(e => new ScannerPackerViewModel
                {
                    Id = e.Id,
                    FullName = e.FullName
                })
                .ToListAsync();
        }
    }
}
