using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Services
{
    public class OrderService : IOrderService
    {
        private readonly StockFlowDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(StockFlowDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Order> CreateOrderAsync(int facilityId, List<OrderItemDto> items)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("Order must contain at least one item.");

            var facilityExists = await _context.Facilities.AnyAsync(f => f.Id == facilityId && f.IsActive);
            if (!facilityExists)
            {
                _logger.LogWarning("Order creation failed: facility not found {FacilityId}", facilityId);
                throw new InvalidOperationException("Selected facility not found.");
            }

            var order = new Order
            {
                FacilityId = facilityId,
                OrderStatus = OrderStatus.Pending,
                CreatedAt = DateTime.Now,
                OrderItems = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            foreach (var itemDto in items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    throw new Exception($"Product with ID {itemDto.ProductId} not found.");

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price, // Snapshot price
                    TotalPrice = product.Price * itemDto.Quantity
                };

                totalAmount += orderItem.TotalPrice;
                order.OrderItems.Add(orderItem);
            }

            order.TotalAmount = totalAmount;

            _context.Orders.Add(order);
            
            // Also init processing record
            var processing = new OrderProcessing { Order = order };
            order.OrderProcessing = processing;
            _context.OrderProcessings.Add(processing);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Order save failed for facility {FacilityId} with {ItemCount} items", facilityId, items.Count);
                throw;
            }
            return order;
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            // Business Logic: Decrease stock when moving to Prepared or Delivered (assuming Prepared is the commitment step)
            // The prompt says: "When order moves to Prepared or Completed, decrease Product.QuantityInStock."
            // We should ensure we only do this ONCE. 
            // If current status is Pending and new is Scanned or Delivered -> Decrease.
            
            if (order.OrderStatus == OrderStatus.Pending && (newStatus == OrderStatus.Scanned || newStatus == OrderStatus.Delivered))
            {
                foreach (var item in order.OrderItems)
                {
                    // Validate stock
                    if (item.Product.QuantityInStock < item.Quantity)
                    {
                        throw new InvalidOperationException($"Not enough stock for product {item.Product.Name}. Available: {item.Product.QuantityInStock}, Requested: {item.Quantity}");
                    }
                    item.Product.QuantityInStock -= item.Quantity;
                }
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ScanOrderAsync(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            // RULE: If Pending -> Scanned
            if (order.OrderStatus == OrderStatus.Pending)
            {
                return await UpdateOrderStatusAsync(orderId, OrderStatus.Scanned);
            }

            // Optional: If Prepared -> Scanned? The prompt says "Subsequent actions will move it to Scanned and Delivered".
            // But the specific scanner rule is "When the scanner inputs the Order ID of an order that is currently Created, the system must automatically change its status to Prepared."
            // It doesn't explicitly say what to do if it is already Prepared.
            // I will assume for now it only handles the Created -> Prepared transition as requested.
            
            return true;
        }
    }
}
