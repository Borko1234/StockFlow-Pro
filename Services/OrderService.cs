using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Data;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Services
{
    public class OrderService : IOrderService
    {
        private readonly FoodieDbContext _context;

        public OrderService(FoodieDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(int facilityId, List<OrderItemDto> items)
        {
            if (items == null || !items.Any())
                throw new ArgumentException("Order must contain at least one item.");

            var order = new Order
            {
                FacilityId = facilityId,
                OrderStatus = OrderStatus.Created,
                CreatedAt = DateTime.Now,
                OrderItems = new List<OrderItem>()
            };

            decimal totalAmount = 0;
            var orderItemsList = new List<OrderItem>();

            foreach (var itemDto in items)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    throw new Exception($"Product with ID {itemDto.ProductId} not found.");

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * itemDto.Quantity
                };

                totalAmount += orderItem.TotalPrice;
                orderItemsList.Add(orderItem);
            }

            order.OrderItems = orderItemsList;
            order.TotalAmount = totalAmount;

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var processing = new OrderProcessing 
            { 
                OrderId = order.Id,
                ProcessDate = DateTime.Now
            };
            _context.OrderProcessings.Add(processing);

            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderProcessing)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            if (order.OrderStatus == OrderStatus.Created && (newStatus == OrderStatus.Scanned || newStatus == OrderStatus.Delivered))
            {
                foreach (var item in order.OrderItems)
                {
                }
            }

            if (newStatus == OrderStatus.Created && order.OrderProcessing != null)
            {
                order.OrderProcessing.PreparedByEmployeeId = null;
                order.OrderProcessing.PreparedByEmployee = null;
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ScanOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            if (order.OrderStatus == OrderStatus.Created)
            {
                // Reduce stock
                foreach (var item in order.OrderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.QuantityInStock -= item.Quantity;
                        if (item.Product.QuantityInStock < 0) item.Product.QuantityInStock = 0; // Prevent negative stock
                    }
                }
                
                order.OrderStatus = OrderStatus.Prepared; // Move to Prepared (Scanned)
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }
}
