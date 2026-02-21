using System;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Models
{
    public class OrderStatusAuditLog
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; }

        public OrderStatus OldStatus { get; set; }
        public OrderStatus NewStatus { get; set; }

        public string AdminUserId { get; set; }
        public string AdminUserName { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
