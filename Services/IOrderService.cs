using System.Collections.Generic;
using System.Threading.Tasks;
using StockFlowPro.Models;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(int facilityId, List<OrderItemDto> items);
        Task<bool> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus);
        Task<bool> ScanOrderAsync(int orderId);
    }

    public class OrderItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
