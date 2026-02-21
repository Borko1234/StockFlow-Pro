using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using StockFlowPro.Models.Enums;

namespace StockFlowPro.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        public int FacilityId { get; set; }
        public Facility? Facility { get; set; }

        public OrderStatus OrderStatus { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public OrderProcessing? OrderProcessing { get; set; }
    }
}
